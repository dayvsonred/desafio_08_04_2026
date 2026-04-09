# Terraform - ReceiveComplaint Lambda

## Caminho
`microservices/receive-complaint/infra`

## Pre-requisitos
- Tabelas DynamoDB aplicadas
- Filas SQS aplicadas
- Zip da Lambda gerado (`lambda_zip_path`)

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\microservices\receive-complaint\infra

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
  -var "classification_queue_arn=<CLASSIFICATION_QUEUE_ARN>" `
  -var "processing_queue_url=<PROCESSING_QUEUE_URL>"
terraform apply `
  -var "lambda_zip_path=<PATH_ZIP>" `
  -var "complaints_table_name=<COMPLAINTS_TABLE_NAME>" `
  -var "complaints_table_arn=<COMPLAINTS_TABLE_ARN>" `
  -var "categories_table_name=<CATEGORIES_TABLE_NAME>" `
  -var "classification_queue_url=<CLASSIFICATION_QUEUE_URL>" `
  -var "classification_queue_arn=<CLASSIFICATION_QUEUE_ARN>" `
  -var "processing_queue_url=<PROCESSING_QUEUE_URL>"
```

## Outputs importantes
- `lambda_name`
- `lambda_invoke_arn`

