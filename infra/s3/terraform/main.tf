terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

resource "aws_s3_bucket" "principal" {
  bucket = var.principal_bucket_name

  tags = merge(var.tags, {
    Name = var.principal_bucket_name
  })
}

resource "aws_s3_bucket_public_access_block" "principal" {
  bucket = aws_s3_bucket.principal.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_ownership_controls" "principal" {
  bucket = aws_s3_bucket.principal.id

  rule {
    object_ownership = "BucketOwnerEnforced"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "principal" {
  bucket = aws_s3_bucket.principal.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_object" "prefixes" {
  for_each = toset(var.folder_prefixes)

  bucket  = aws_s3_bucket.principal.id
  key     = "${trim(each.value, "/")}/"
  content = ""
}
