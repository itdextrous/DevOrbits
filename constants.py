
# Document categories
RECOGNIZED_DOCUMENT_NAMES = {
    'certificate_of_origin_for_vehicle', 'CertificateOfTitle', 'dr_123', 'driver_license',
    'dtf_802', 'dtf_803', 'dtf_804', 'insurance_identification_card', 'mv_82',
    'mv_103', 'mv_900', 'POA', 'vehicle_buyer_order'
}

DTF_GROUP = {'dtf_802', 'dtf_803', 'dtf_804'}
COT_CV = {'certificate_of_origin_for_vehicle', 'CertificateOfTitle'}

# Error messages
ERROR_MESSAGES = {
    'no_file_part': 'No file part in the request',
    'no_file_uploaded': 'No file uploaded',
    'invalid_image_format': 'Invalid image file format or no file selected',
    'text_extraction_failed': 'Failed to extract text from image',
    'unicode_decode_error': 'Unable to decode text from the document'
}
