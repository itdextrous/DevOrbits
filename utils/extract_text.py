import requests
import json
import concurrent.futures
import os
from dotenv import load_dotenv

load_dotenv()

azure_endpoint = "https://"+os.getenv('AZURE_AI_SERVICES_ENDPOINT') + "/vision/v3.2/read/analyze" 
azure_api_key = os.getenv('AZURE_AI_SERVICES_KEY') 

def extract_text_from_image(image_path, language='en', overlay=False):
    try:
        listOfPageText = []
        headers = {
            'Ocp-Apim-Subscription-Key': azure_api_key,
            'Content-Type': 'application/octet-stream'
        }
        
        with open(image_path, 'rb') as f:
            response = requests.post(azure_endpoint, headers=headers, data=f)
        
        if response.status_code != 202:
            return f"Error: {response.status_code}, {response.text}"
        
        operation_url = response.headers["Operation-Location"]
        
        # Poll for results
        while True:
            analysis_response = requests.get(operation_url, headers={'Ocp-Apim-Subscription-Key': azure_api_key})
            analysis = analysis_response.json()
            if analysis.get("status") == "succeeded":
                break
        
        # Process extracted text
        listOfPageText = [
            "\n".join(line["text"] for line in page.get("lines", []))
            for page in analysis.get("analyzeResult", {}).get("readResults", [])
        ]
        
        print(listOfPageText)
        return listOfPageText
    except Exception as e:
        return str(e)

