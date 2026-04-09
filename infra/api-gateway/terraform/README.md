# Terraform - API Gateway

## Caminho
`infra/api-gateway/terraform`

## Pre-requisito
Deploy das Lambdas aplicado, com:
- `receive_lambda_invoke_arn`
- `receive_lambda_name`
- `metrics_lambda_invoke_arn`
- `metrics_lambda_name`

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\api-gateway\terraform

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan `
  -var "receive_lambda_invoke_arn=<RECEIVE_INVOKE_ARN>" `
  -var "receive_lambda_name=<RECEIVE_FUNCTION_NAME>" `
  -var "metrics_lambda_invoke_arn=<METRICS_INVOKE_ARN>" `
  -var "metrics_lambda_name=<METRICS_FUNCTION_NAME>"
terraform apply `
  -var "receive_lambda_invoke_arn=<RECEIVE_INVOKE_ARN>" `
  -var "receive_lambda_name=<RECEIVE_FUNCTION_NAME>" `
  -var "metrics_lambda_invoke_arn=<METRICS_INVOKE_ARN>" `
  -var "metrics_lambda_name=<METRICS_FUNCTION_NAME>"
```

## Outputs
- `complaints_route`
- `metrics_route`
