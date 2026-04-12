# Terraform - API Gateway

## Caminho
`infra/api-gateway/terraform`

## Pre-requisito
Deploy das 4 Lambdas aplicado na mesma conta/regiao.

Esse modulo tambem configura CORS para browser:
- `allow_origins = ["*"]`
- `allow_methods = ["GET", "POST", "OPTIONS"]`
- `allow_headers = ["content-type", "x-correlation-id"]`

As referencias de lambdas (nome e ARN) ja estao fixas no `main.tf`:

1. `complaint-classifier-phase1-receive-complaint`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-receive-complaint`
2. `complaint-classifier-phase1-classify-complaint`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-classify-complaint`
3. `complaint-classifier-phase1-process-classified-complaint`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-process-classified-complaint`
4. `complaint-classifier-phase1-daily-complaint-metrics`
   `arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-daily-complaint-metrics`

## Manter API existente (ID `3wzhy48jd6`)
Use import apenas uma vez, se o recurso ainda nao estiver no state.

```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\api-gateway\terraform
terraform init
terraform import aws_apigatewayv2_api.http_api 3wzhy48jd6
```

Se der erro `Resource already managed by Terraform`, nao rode `import` novamente.
Nesse caso, siga direto com `terraform plan` e `terraform apply`.

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
- `api_id`
- `api_endpoint`
- `complaints_route`
- `metrics_route`
- `lambda_references`
