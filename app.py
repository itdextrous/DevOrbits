from flask import Flask, request, jsonify, send_from_directory,redirect
from config import Config
from utils.file_utils import create_directory_if_not_exists
from autharization.secure_auth import token_required
from autharization.validators import *
from utils.predict_label import *
from constants import *
from utils.file_utils import generate_short_id
from utils.file_uploader import *
from apscheduler.schedulers.background import BackgroundScheduler
from autharization.validators import *
from werkzeug.middleware.proxy_fix import ProxyFix
from flask_cors import CORS

# Initialize Flask app
app = Flask(__name__)

# Enable ProxyFix to trust Azure's HTTPS headers
app.wsgi_app = ProxyFix(app.wsgi_app, x_proto=1, x_host=1)

# Ensure URLs are generated with HTTPS
app.config['PREFERRED_URL_SCHEME'] = 'https'

CORS(app, origins="*")
app.config.from_object(Config)

# Create necessary directories
create_directory_if_not_exists(Config.UPLOAD_FOLDER)
create_directory_if_not_exists(Config.SPLIT_FOLDER)

# Schedule the deletion task every 5 minutes
scheduler = BackgroundScheduler()
scheduler.add_job(func=lambda: delete_old_files(Config.UPLOAD_FOLDER), trigger="interval", minutes=60)
scheduler.add_job(func=lambda: delete_old_files(Config.SPLIT_FOLDER), trigger="interval", minutes=60)
scheduler.start()


@app.before_request
def before_request():
    if not request.is_secure:
        url = request.url.replace('http://', 'https://', 1)
        return redirect(url, code=301)


@app.route('/')
def index():
    return 'OCR EXTRACTER IS RUNNING v0.002'


@app.route('/upload', methods=['POST'])
@token_required
@validate_file_types(allowed_file_types=('.pdf', '.png', '.jpg', '.jpeg'))
def upload_file():
    uniqueId = generate_short_id()
    ignorpages_path = get_file_path(f"Ignorepages_{uniqueId}.pdf", 'SPLIT_FOLDER')
    auth = request.form.get('authNumber')
    base_url = request.form.get('baseUrl')
    token = request.form.get('token')
    requestId = request.form.get('requestId')
    web_cookie = request.headers.get('webcookie')
    remove_file_if_exists(ignorpages_path)
    pdf, image_files = request.files.get('file'), request.files.getlist('images')
    if not pdf and not image_files:
        return jsonify({'status_code': 400, 'error': 'No files uploaded'})
    if pdf:
        responses = handle_pdf_upload(pdf, uniqueId) if pdf else []    
        # print(responses,'responses')
    else:        
        responses = handle_image_upload(image_files, uniqueId)    
    report_filename = generate_report(responses['responses'], uniqueId, request.form, responses.get('unrecognized_documents', []))
    return jsonify({
        'status_code': 200,
        'data': responses['responses'],
        'report_file': report_filename,
        'auth': auth,
        'baseUrl': base_url,
        'token': token,
        'uniqueId': uniqueId,
        'requestId': requestId,
        'web_cookie': web_cookie,
        'unrecognized_documents': responses.get('unrecognized_documents', [])
    })

@app.route('/upload_unrecognized', methods=['POST'])
@token_required
@validate_file_types(allowed_file_types=('.pdf', '.png', '.jpg', '.jpeg'))
def upload_unrecognized():
    form_data = request.form
    auth, base_url, token, requestId = form_data.get('authNumber'), form_data.get('baseUrl'), form_data.get('token'), form_data.get('requestId')
    unrecognized_documents = [doc.strip() for doc in form_data.get('unrecognized_documents', '').split(',')]
    if 'file' not in request.files or not request.files['file']:
        return jsonify({'status_code': 400, 'error': 'No file part in the request'})
    file = request.files['file']
    if file.filename.endswith('.pdf'):
        return handle_pdf_request(file, unrecognized_documents, auth, base_url, token, requestId)
    elif allowed_file(file.filename):
        return handle_image_request(file, unrecognized_documents, auth, base_url, token, requestId)
    return jsonify({'status_code': 400, 'error': 'Invalid file type or missing file'}), 400

@app.route('/get-report', methods=['GET','POST'])
@token_required
def get_report():
    report_id = request.args.get('uniqueId')
    if not report_id:
        return jsonify({'status_code': 400, 'error': 'uniqueId is missing'})
    json_file_path = os.path.join(app.config['UPLOAD_FOLDER'], f'{report_id}_report.json')    
    try:
        with open(json_file_path) as json_file:
            data = json.load(json_file)
        response = {
            'status_code': 200,
            'auth': data.get('auth'),
            'baseUrl': data.get('baseUrl'),
            'token': data.get('token'),
            'data': data.get('data'),
            'filename': data.get('file_name'),
            'requestId': data.get('requestId'),
            'web_cookie': data.get('web_cookie'),
            'unrecognized_documents': data.get('unrecognized_documents')
        }
        print('Response:', response)
        return jsonify(response)    
    except FileNotFoundError:
        return jsonify({'status_code': 404, 'error': 'JSON file not found'})
    except Exception as e:
        return jsonify({'status_code': 500, 'error': str(e)})

@app.route('/get_pdf_split/<filename>')
def get_pdf_split(filename):
    return send_from_directory(Config.SPLIT_FOLDER, filename)

@app.route('/get_uploaded_pdf/<filename>')
def get_uploaded_pdf(filename):
    return send_from_directory(Config.UPLOAD_FOLDER, filename)

@app.route('/uploads/<filename>')
def get_pdf(filename):
    return send_from_directory(Config.UPLOAD_FOLDER, filename)

@app.route('/clear_pdfs', methods=['POST'])
@token_required
def clear_pdfs():
    try:
        # Clear UPLOAD_FOLDER
        clear_files_in_directory(Config.UPLOAD_FOLDER)

        # Clear SPLIT_FOLDER
        clear_files_in_directory(Config.SPLIT_FOLDER)

        return jsonify({'message': 'All PDFs have been cleared.'}), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500


if __name__ == '__main__':
    # app.run(host='0.0.0.0', port=5000, ssl_context=('cert.pem', 'key.pem'))
    app.run(host='0.0.0.0', port=5000)
    

