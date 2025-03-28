import os
import joblib
class Config:
    SECRET_KEY = 'your_secret_key'
    SPLIT_FOLDER = 'assets/split_pdfs' 
    UPLOAD_FOLDER = 'assets/uploads'
    ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg'}   
    IGNORE_PAGES_FILE_PREFIX = "ignorpages_"
    REPORT_FILE_SUFFIX = "_DMP_Report.json"
    DOCUMENT_MODEL= joblib.load('document_classifier.pkl')
    VECTORIZER_MODEL= joblib.load('tfidf_vectorizer.pkl')
    IMAGE_UPLOAD_FILE_PATH_TEMPLATE = os.path.join(UPLOAD_FOLDER, '{filename}')
    IMAGE_SPLIT_FILE_PATH_TEMPLATE = os.path.join(SPLIT_FOLDER, '{filename}')
