# Terraform - DailyComplaintMetrics Lambda

## Caminho
`microservices/daily-complaint-metrics/infra`

## Pre-requisitos
- Tabela DynamoDB de metricas diarias aplicada
- Fila SQS de metricas aplicada
- Zip da Lambda gerado (`lambda_zip_path`)

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\microservices\daily-complaint-metrics\infra

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan `
  -var "lambda_zip_path=<PATH_ZIP>" `
  -var "daily_metrics_table_name=<DAILY_METRICS_TABLE_NAME>" `
  -var "daily_metrics_table_arn=<DAILY_METRICS_TABLE_ARN>" `
  -var "metrics_queue_arn=<METRICS_QUEUE_ARN>"
terraform apply `
  -var "lambda_zip_path=<PATH_ZIP>" `
  -var "daily_metrics_table_name=<DAILY_METRICS_TABLE_NAME>" `
  -var "daily_metrics_table_arn=<DAILY_METRICS_TABLE_ARN>" `
  -var "metrics_queue_arn=<METRICS_QUEUE_ARN>"
```

## Outputs importantes
- `lambda_name`
- `lambda_invoke_arn`
