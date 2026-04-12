# Terraform - Frontend (Angular)

## Caminho
`front/infra`

## O que este modulo cria
- distribuicao CloudFront com OAC (Origin Access Control)
- policy de leitura no bucket S3 compartilhado para o CloudFront
- upload dos arquivos gerados em `front/dist/front` para um prefix
- fallback SPA (`/index.html`) para erros 403/404

## Pre-requisitos
1. Bucket central criado em `infra/s3/terraform`:
   - `itau-data-teste-20260411`
2. Build do frontend gerado:
   - `front/dist/front`
3. Credenciais AWS configuradas:
   - mesma conta onde esta o API Gateway

## Uso (PowerShell)
```powershell
cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\infra\s3\terraform
terraform init
terraform apply

cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\front
npm install
npm run build

cd C:\Users\niore\Documents\desafio\desafio_08_04_2026\front\infra
$env:AWS_PROFILE="default"
$env:AWS_REGION="sa-east-1"

terraform init
terraform plan
terraform apply
```

## Variaveis uteis
- `shared_bucket_name` (padrao `itau-data-teste-20260411`)
- `frontend_prefix` (padrao `frontend`)
- `frontend_build_dir` (padrao `../dist/front`)
- `cloudfront_price_class` (`PriceClass_100`, `PriceClass_200`, `PriceClass_All`)

## Outputs
- `frontend_url`
- `cloudfront_distribution_id`
- `cloudfront_domain_name`
- `frontend_bucket_name`
- `frontend_prefix`
- `uploaded_files_count`
