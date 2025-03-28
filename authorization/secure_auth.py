from flask import request, jsonify
from functools import wraps
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.backends import default_backend
import base64
from dotenv import load_dotenv
import os
load_dotenv()


KEY = os.getenv('KEY', '').encode('utf-8')
IV = os.getenv('IV', '').encode('utf-8')

def encrypt_data(data):
    """Encrypt data using AES CBC mode."""
    padder = padding.PKCS7(algorithms.AES.block_size).padder()
    padded_data = padder.update(data.encode('utf-8')) + padder.finalize()    
    cipher = Cipher(algorithms.AES(KEY), modes.CBC(IV), backend=default_backend())
    encryptor = cipher.encryptor()
    encrypted_data = encryptor.update(padded_data) + encryptor.finalize()    
    return base64.b64encode(encrypted_data).decode('utf-8')


def decrypt_data(encrypted_data):
    """Decrypt data using AES CBC mode."""
    encrypted_data = base64.b64decode(encrypted_data)
    cipher = Cipher(algorithms.AES(KEY), modes.CBC(IV), backend=default_backend())
    decryptor = cipher.decryptor()
    padded_data = decryptor.update(encrypted_data) + decryptor.finalize()    
    unpadder = padding.PKCS7(algorithms.AES.block_size).unpadder()
    data = unpadder.update(padded_data) + unpadder.finalize()
    print("data",data)
    return data.decode('utf-8')


def validate_token(encrypted_token):
    """Validate the token by decrypting it."""
    try:
        decrypted_data = decrypt_data(encrypted_token)
        decrypt_key = os.getenv('DECRIPTED_KEY')
        if decrypted_data == decrypt_key :
            return True
        else:
            return False
    except Exception as e:
        print("Exception occurred ")
        print(e)
        return False


def token_required(f):
    """Decorator to protect routes with token validation."""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        encrypted_token = request.args.get('auth') or request.form.get('auth')
        print(f"Encrypted Token: {encrypted_token}")         
        if not encrypted_token:
            return jsonify({"error": "AuthToken is missing", "status_code": 404})
        valid = validate_token(encrypted_token)        
        if not valid:
            return jsonify({"error": "Invalid AuthToken", "status_code": 401})        
        return f(*args, **kwargs)    
    return decorated_function
