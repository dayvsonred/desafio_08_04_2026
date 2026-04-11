# Terraform - API Gateway

## Caminho
`infra/api-gateway/terraform`

## Pre-requisito
Deploy das 4 Lambdas aplicado na mesma conta/regiao.

As referencias de lambdas (nome e ARN) ja estao fixas no `main.tf`:

1. `complaint-classifier-phase1-receive-complaint`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-receive-complaint`
2. `complaint-classifier-phase1-classify-complaint`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-classify-complaint`
3. `complaint-classifier-phase1-process-classified-complaint`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-process-classified-complaint`
4. `complaint-classifier-phase1-daily-complaint-metrics`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-daily-complaint-metrics`

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\api-gateway\terraform

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan
terraform apply
```

## Outputs
- `complaints_route`
- `metrics_route`
- `lambda_references`
