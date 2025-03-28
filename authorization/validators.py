from functools import wraps
from flask import request, jsonify
import time
from config import *
def validate_file_types(allowed_file_types):
    def decorator(func):
        @wraps(func)
        def wrapper(*args, **kwargs):
            files = request.files

            # Check for PDF and image file types
            for file_key in ['file'] + [key for key in files if key.startswith('images')]:
                file = files.get(file_key)
                if file and not file.filename.lower().endswith(allowed_file_types):
                    return jsonify({'status_code': 400, 'error': f'Invalid file type: {file.filename}'}), 400

            return func(*args, **kwargs)
        return wrapper
    return decorator


def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in Config.ALLOWED_EXTENSIONS

import os
from typing import Any

def remove_file_if_exists(file_path: Any) -> None:
    """Removes the file if it exists."""
    try:
        if os.path.exists(file_path):
            os.remove(file_path)
            print(f"Removed file: {file_path}")
        else:
            print(f"File does not exist: {file_path}")
    except Exception as e:
        print(f"Error removing file {file_path}: {str(e)}")


def delete_old_files(folder, time_limit_in_minutes=30):
    now = time.time()
    time_limit = time_limit_in_minutes * 60  # Convert minutes to seconds

    for filename in os.listdir(folder):
        file_path = os.path.join(folder, filename)
        if os.path.isfile(file_path):
            file_age = now - os.path.getmtime(file_path)
            if file_age > time_limit:
                try:
                    os.remove(file_path)
                    print(f"Deleted {file_path} as it was older than {time_limit_in_minutes} minutes.")
                except Exception as e:
                    print(f"Error deleting {file_path}: {str(e)}")