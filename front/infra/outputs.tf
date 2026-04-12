output "frontend_bucket_name" {
  value = data.aws_s3_bucket.principal.bucket
}

output "cloudfront_distribution_id" {
  value = aws_cloudfront_distribution.frontend.id
}

output "cloudfront_domain_name" {
  value = aws_cloudfront_distribution.frontend.domain_name
}

output "frontend_url" {
  value = "https://${aws_cloudfront_distribution.frontend.domain_name}"
}

output "uploaded_files_count" {
  value = length(local.frontend_files)
}

output "frontend_prefix" {
  value = local.frontend_prefix
}
