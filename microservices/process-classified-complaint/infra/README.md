# Terraform - ProcessClassifiedComplaint Lambda

## Caminho
`microservices/process-classified-complaint/infra`

## Pre-requisitos
- Tabelas DynamoDB aplicadas
- Filas SQS aplicadas
- Zip da Lambda gerado (`lambda_zip_path`)

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\microservices\process-classified-complaint\infra

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan `
  -var "lambda_zip_path=<PATH_ZIP>" `
  -var "complaints_table_name=<COMPLAINTS_TABLE_NAME>" `
  -var "complaints_table_arn=<COMPLAINTS_TABLE_ARN>" `
  -var "categories_table_name=<CATEGORIES_TABLE_NAME>" `
  -var "classification_queue_url=<CLASSIFICATION_QUEUE_URL>" `
  -var "processing_queue_url=<PROCESSING_QUEUE_URL>" `
  -var "processing_queue_arn=<PROCESSING_QUEUE_ARN>"
terraform apply `
  -var "lambda_zip_path=<PATH_ZIP>" `
  -var "complaints_table_name=<COMPLAINTS_TABLE_NAME>" `
  -var "complaints_table_arn=<COMPLAINTS_TABLE_ARN>" `
  -var "categories_table_name=<CATEGORIES_TABLE_NAME>" `
  -var "classification_queue_url=<CLASSIFICATION_QUEUE_URL>" `
  -var "processing_queue_url=<PROCESSING_QUEUE_URL>" `
  -var "processing_queue_arn=<PROCESSING_QUEUE_ARN>"
```

