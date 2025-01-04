import requests
import json

url = 'http://127.0.0.1:3000/kitauji_api'

data = {
    'apiType': 1,
    'userId': 1234
}
json_data = json.dumps(data)

headers = {
    'Content-Type': 'application/json'
}

print(json_data)
response = requests.post(url, headers=headers, data=json_data)
print(response.text)

response_data = json.loads(response.text)
print(response_data["infoIdList"])
