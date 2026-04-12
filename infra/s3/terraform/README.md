# Terraform - S3 Centralizado

## Caminho
`infra/s3/terraform`

## Objetivo
Criar o bucket S3 principal do projeto e centralizar os prefixes usados pelos servicos.

Bucket principal padrao:
- `itau-data-teste-20260411`

## Observacao importante
No S3 nao existe "bucket dentro de bucket".  
Para centralizacao, usamos um bucket unico com prefixes (pastas logicas), por exemplo:
- `complaint_message_received/`
- `complaint_message_processed/`
- `frontend/`

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\s3\terraform

$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan
terraform apply
```

## Se o bucket ja existir
Importe antes do apply para evitar tentativa de recriacao:

```powershell
terraform init
terraform import aws_s3_bucket.principal itau-data-teste-20260411
```

## Outputs
- `principal_bucket_name`
- `principal_bucket_arn`
- `folder_prefixes`
