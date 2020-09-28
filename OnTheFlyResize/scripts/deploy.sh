#!/bin/sh
source .env
yc serverless function create --name=$FUNCTION_NAME
if [[ ! -e "build" ]]; then
    mkdir "build"
fi

cp *.cs ./build
cp *.csproj ./build


yc serverless function version create \
  --function-name=$FUNCTION_NAME \
  --runtime dotnetcore31-preview \
  --entrypoint Handler \
  --memory 128m \
  --execution-timeout 30s \
  --source-path ./build\
  --environment AWS_ACCESS_KEY=$AWS_ACCESS_KEY,AWS_SECRET_KEY=$AWS_SECRET_KEY,BUCKET=$BUCKET,PREFIX=$PREFIX


rm -rf ./build