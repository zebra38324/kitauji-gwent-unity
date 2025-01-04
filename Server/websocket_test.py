import asyncio
import json
import websockets

async def send_json(uri):
    # 将Python字典转换为JSON字符串
    data = data = {
        'apiType': 1,
        'userId': 1234
    }
    json_data = json.dumps(data)
    
    # 建立WebSocket连接
    async with websockets.connect(uri) as websocket:
        # 发送JSON字符串
        await websocket.send(json_data)
        
        # 接收服务器响应
        response = await websocket.recv()
        print(f"Received: {response}")

# WebSocket服务器地址
uri = "ws://localhost:3000"

# 运行客户端
asyncio.run(send_json(uri))
