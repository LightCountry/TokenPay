wget -O https://github.com/LightCountry/TokenPay/releases/download/v1.0.6.5/linux-x64.zip

unzip -o linux-x64.zip


#如果文件夹不存在，创建文件夹
if [ ! -d "prod" ]; then
    mkdir -p prod
    cp appsettings.Example.json prod/appsettings.json
fi

#创建空数据文件,方便挂载文件
if [ ! -e prod/TokenPay.db ]; then
    touch prod/TokenPay.db
fi



cat << EOF > Dockerfile
FROM  mcr.microsoft.com/dotnet/runtime
COPY . /app
WORKDIR /app
RUN ls
CMD ["./TokenPay","--urls=http://+:5001"]
EOF

cat << EOF > prod/docker-compose.yml
version: "3.7"

services:
  # bot业务
  tokenpay:
    image: tokenpay
    network_mode: host
    container_name: tokenpay
    restart: always
    volumes:
      - ./appsettings.json:/app/appsettings.json
      - ./TokenPay.db:/app/TokenPay.db
    environment:
      TZ: Asia/Shanghai
EOF

#执行docker 编译命令
docker build -t tokenpay .
GREEN='\033[32m'
RESET='\033[0m'
echo -e "${GREEN}编译docker镜像完成,请进入prod目录,编辑appsettings.json ${RESET}"
echo -e "${GREEN}编辑完成后,docker compose up -d 启动容器 ,访问ip:5001  ${RESET}"