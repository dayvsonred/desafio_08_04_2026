# Terraform - ClassifyComplaint Lambda

## Caminho
`microservices/classify-complaint/infra`

## Pre-requisitos
- Tabelas DynamoDB aplicadas
- Filas SQS aplicadas
- Bucket S3 central aplicado (`infra/s3/terraform`): `itau-data-teste-20260411`
- Zip da Lambda gerado (`lambda_zip_path`)
- Credenciais AWS ativas no terminal (`aws login` / `AWS_PROFILE`)

## Descoberta automatica de recursos AWS
- DynamoDB e SQS sao buscados automaticamente por `project_name`
- Prefixo padrao: `complaint-classifier-phase1`

## Uso (PowerShell)
```powershell
$Root = "C:\Users\niore\Documents\desafio\desafio_08_04_2026"
$ProjectName = "complaint-classifier-phase1"
$ZipPath = "$Root\microservices\classify-complaint\infra\classify-complaint.zip"

# Configurar (one-time)
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

# Gerar ZIP da Lambda
dotnet lambda package --project-location "$Root\microservices\classify-complaint\ClassifyComplaint.Function" --configuration Release --framework net8.0 --output-package "$ZipPath"

cd "$Root\microservices\classify-complaint\infra"
terraform init
terraform plan `
  -var "project_name=$ProjectName" `
  -var "lambda_zip_path=$ZipPath"
terraform apply `
  -var "project_name=$ProjectName" `
  -var "lambda_zip_path=$ZipPath"
```

Para usar outro prefixo de recursos AWS, altere apenas o valor de `$ProjectName`.
