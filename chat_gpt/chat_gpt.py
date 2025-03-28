import http.client
import json
import time
import os
from dotenv import load_dotenv

# Load environment variables
load_dotenv()
azure_openai_endpoint = os.getenv("AZURE_AI_SERVICES_ENDPOINT")
azure_openai_api_key = os.getenv("AZURE_AI_SERVICES_KEY")
azure_openai_deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT")  
azure_openai_api_version = os.getenv("AZURE_OPENAI_API_VERSION")

def getDataWithOpenAI(text, prompt=""):
    chunk_size = 40000  # Define the size of each chunk

    # Split the text into chunks
    chunks = [text[i:i+chunk_size] for i in range(0, len(text), chunk_size)]

    results = []  # Store results from each chunk
    count = 0
    for chunk in chunks:
        count += 1
        if not prompt:
            prompt_text = f"""
                [The Text]{chunk}
                
                [Instructions on What and How to Process]
                    - Analyze the content in the [The Text] section.
                    - Extract the personal details from the content.
                    - Extract the details from any type of document.
                    - Extract the business details from content.
                    - Extract the vehicle details from content.
                    - Extract the checkbox value from text if available.
                    - Don't add any extra information.
                    - On the top of response give document title.like Document title
                    
                [How to return processed content]
                
                return the content in json format key:value pair
                """
        else:
            prompt_text = f"[The Text]{chunk} {prompt}"

        response_data = send_request_with_retry(prompt_text)
        results.append(response_data)
    return results

def send_request_with_retry(prompt_text):
    retry_delay = 1  # Initial retry delay in seconds
    max_retries = 5
    retries = 0
    response = None
    while retries < max_retries:
        try:
            # Generate completion using Azure OpenAI API
            conn = http.client.HTTPSConnection(azure_openai_endpoint.replace("https://", "").replace("http://", ""))
            payload = json.dumps({
                "messages": [
                    {"role": "system", "content": prompt_text}
                ],
                "temperature": 0.5
            })
            headers = {
                'Content-Type': 'application/json',
                'api-key': azure_openai_api_key  # Use Azure OpenAI API Key
            }
            path = f"/openai/deployments/{azure_openai_deployment}/chat/completions?api-version={azure_openai_api_version}"
            conn.request("POST", path, payload, headers)
            response = conn.getresponse()

            # Read and decode the response
            response_data = response.read().decode('utf-8')
            response_json = json.loads(response_data)

            return response_json
        
        except Exception as e:
            if response and response.status == 429:  # Rate limit exceeded
                time.sleep(retry_delay)
                retry_delay *= 2  # Exponential backoff
                retries += 1
            else:
                raise e  # Re-raise other exceptions

    raise Exception("Max retries exceeded. Unable to complete request.")
