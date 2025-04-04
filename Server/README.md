# 服务端代码
## 环境
node，v20.18.1

### 运行
Server路径下运行。带ssl后缀为启用wss。prod/dev控制数据库使用正式环境还是开发环境的。

需要ssl时，自行配置`src/server.js`中的证书路径。
```shell
npm run prod_ssl/prod/dev_ssl/dev
```

### 测试
Server路径下运行
```shell
npm run test
```

### linux部署
服务器部署时，使用pm2运行，保证进程常驻
```shell
# 安装nvm与指定版本node
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.3/install.sh | bash
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
nvm install 20.18.1
nvm use 20.18.1

# 安装所需配置
npm install

# 使用pm2运行（可能需要sudo）
npm install pm2 -g
npm run prod_pm2_ssl/dev_pm2_ssl

# 查看运行状态及日志
pm2 status kitauji_server_prod/kitauji_server_dev
pm2 log kitauji_server_prod/kitauji_server_dev

# 停止
pm2 stop kitauji_server_prod/kitauji_server_dev
```
