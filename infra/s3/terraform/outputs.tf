output "principal_bucket_name" {
  value = aws_s3_bucket.principal.bucket
}

output "principal_bucket_arn" {
  value = aws_s3_bucket.principal.arn
}

output "folder_prefixes" {
  value = [for prefix in var.folder_prefixes : "${trim(prefix, "/")}/"]
}
