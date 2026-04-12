# Terraform - ReceiveComplaint Lambda

## Caminho
`microservices/receive-complaint/infra`

## Pre-requisitos
- Tabelas DynamoDB aplicadas
- Filas SQS aplicadas
- Bucket S3 central aplicado (`infra/s3/terraform`): `itau-data-teste-20260411`
- Zip da Lambda gerado (`lambda_zip_path`)
- Credenciais AWS ativas no terminal (`aws login` / `AWS_PROFILE`)

## Descoberta automatica de recursos AWS
- DynamoDB e SQS sao buscados automaticamente por `project_name`
- Prefixo padrao: `complaint-classifier-phase1`
- Exemplo de recursos esperados:
  - `complaint-classifier-phase1-complaints`
  - `complaint-classifier-phase1-categories`
  - `complaint-classifier-phase1-classification`
  - `complaint-classifier-phase1-processing`
  - `complaint-classifier-phase1-metrics`

## Uso (PowerShell)
```powershell
$Root = "C:\Users\niore\Documents\desafio\desafio_08_04_2026"
$ProjectName = "complaint-classifier-phase1"
$ZipPath = "$Root\microservices\receive-complaint\infra\receive-complaint.zip"

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

# Gerar ZIP da Lambda
dotnet lambda package --project-location "$Root\microservices\receive-complaint\ReceiveComplaint.Function" --configuration Release --framework net8.0 --output-package "$ZipPath"

cd "$Root\microservices\receive-complaint\infra"
terraform init
terraform plan `
  -var "project_name=$ProjectName" `
  -var "lambda_zip_path=$ZipPath"
terraform apply `
  -var "project_name=$ProjectName" `
  -var "lambda_zip_path=$ZipPath"
```

Para usar outro prefixo de recursos AWS, altere apenas o valor de `$ProjectName`.

## Outputs importantes
- `lambda_name`
- `lambda_invoke_arn`
