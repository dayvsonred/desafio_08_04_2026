variable "aws_region" {
  type        = string
  description = "AWS region for CloudFront/S3 integration resources"
  default     = "sa-east-1"
}

variable "project_name" {
  type        = string
  description = "Project prefix used in frontend infrastructure names"
  default     = "complaint-classifier-phase1"
}

variable "shared_bucket_name" {
  type        = string
  description = "Central S3 bucket name created in infra/s3/terraform"
  default     = "itau-data-teste-20260411"
}

variable "frontend_prefix" {
  type        = string
  description = "Prefix (folder) used to store frontend files in shared bucket"
  default     = "frontend"
}

variable "frontend_build_dir" {
  type        = string
  description = "Path to Angular build output directory"
  default     = "../dist/front"
}

variable "cloudfront_price_class" {
  type        = string
  description = "CloudFront price class"
  default     = "PriceClass_100"

  validation {
    condition     = contains(["PriceClass_100", "PriceClass_200", "PriceClass_All"], var.cloudfront_price_class)
    error_message = "cloudfront_price_class must be one of: PriceClass_100, PriceClass_200, PriceClass_All."
  }
}

variable "tags" {
  type        = map(string)
  description = "Additional tags for frontend resources"
  default     = {}
}
