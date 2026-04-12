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

data "aws_s3_bucket" "principal" {
  bucket = var.shared_bucket_name
}

data "aws_cloudfront_cache_policy" "caching_optimized" {
  name = "Managed-CachingOptimized"
}

locals {
  frontend_build_dir = abspath(var.frontend_build_dir)
  frontend_files     = fileset(local.frontend_build_dir, "**")
  frontend_prefix    = trim(var.frontend_prefix, "/")
  origin_path        = local.frontend_prefix == "" ? null : "/${local.frontend_prefix}"

  content_types = {
    ".css"   = "text/css"
    ".gif"   = "image/gif"
    ".html"  = "text/html; charset=utf-8"
    ".ico"   = "image/x-icon"
    ".jpeg"  = "image/jpeg"
    ".jpg"   = "image/jpeg"
    ".js"    = "application/javascript"
    ".json"  = "application/json"
    ".map"   = "application/json"
    ".png"   = "image/png"
    ".svg"   = "image/svg+xml"
    ".txt"   = "text/plain; charset=utf-8"
    ".webp"  = "image/webp"
    ".woff"  = "font/woff"
    ".woff2" = "font/woff2"
  }
}

resource "aws_cloudfront_origin_access_control" "frontend" {
  name                              = "${var.project_name}-front-oac"
  description                       = "OAC for ${var.shared_bucket_name}"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

resource "aws_cloudfront_distribution" "frontend" {
  enabled             = true
  is_ipv6_enabled     = true
  comment             = "${var.project_name} frontend"
  default_root_object = "index.html"
  price_class         = var.cloudfront_price_class

  origin {
    domain_name              = data.aws_s3_bucket.principal.bucket_regional_domain_name
    origin_id                = "frontend-s3-origin"
    origin_access_control_id = aws_cloudfront_origin_access_control.frontend.id
    origin_path              = local.origin_path
  }

  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "frontend-s3-origin"
    viewer_protocol_policy = "redirect-to-https"
    compress               = true
    cache_policy_id        = data.aws_cloudfront_cache_policy.caching_optimized.id
  }

  custom_error_response {
    error_code            = 403
    response_code         = 200
    response_page_path    = "/index.html"
    error_caching_min_ttl = 0
  }

  custom_error_response {
    error_code            = 404
    response_code         = 200
    response_page_path    = "/index.html"
    error_caching_min_ttl = 0
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  tags = merge(var.tags, {
    Name = "${var.project_name}-front-distribution"
  })
}

data "aws_iam_policy_document" "frontend_bucket_policy" {
  statement {
    sid    = "AllowCloudFrontReadOnly"
    effect = "Allow"

    actions = ["s3:GetObject"]
    resources = [
      local.frontend_prefix == ""
      ? "${data.aws_s3_bucket.principal.arn}/*"
      : "${data.aws_s3_bucket.principal.arn}/${local.frontend_prefix}/*"
    ]

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [aws_cloudfront_distribution.frontend.arn]
    }
  }
}

resource "aws_s3_bucket_policy" "frontend" {
  bucket = data.aws_s3_bucket.principal.id
  policy = data.aws_iam_policy_document.frontend_bucket_policy.json
}

resource "aws_s3_object" "frontend_files" {
  for_each = { for file in local.frontend_files : file => file }

  bucket = data.aws_s3_bucket.principal.id
  key    = local.frontend_prefix == "" ? each.value : "${local.frontend_prefix}/${each.value}"
  source = "${local.frontend_build_dir}/${each.value}"
  etag   = filemd5("${local.frontend_build_dir}/${each.value}")

  cache_control = each.value == "index.html" ? "no-cache, no-store, must-revalidate" : "public, max-age=31536000, immutable"
  content_type  = lookup(local.content_types, lower(try(regex("\\.[^.]+$", each.value), "")), "application/octet-stream")
}
