#!/bin/bash
set -e

awslocal s3 mb s3://mymarina-local --region us-east-1

awslocal s3api put-bucket-cors --bucket mymarina-local --cors-configuration '{
  "CORSRules": [{
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5174",
      "http://localhost:4321"
    ],
    "AllowedMethods": ["GET", "PUT", "POST", "DELETE", "HEAD"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"]
  }]
}'

echo "LocalStack: bucket mymarina-local created and CORS configured"
