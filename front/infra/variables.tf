variable "aws_region" {
  type        = string
  description = "AWS region for S3 bucket resources"
  default     = "sa-east-1"
}

variable "project_name" {
  type        = string
  description = "Project prefix used in frontend infrastructure names"
  default     = "complaint-classifier-phase1"
}

variable "frontend_build_dir" {
  type        = string
  description = "Path to Angular build output directory"
  default     = "../dist/front"
}

variable "frontend_bucket_name" {
  type        = string
  description = "Optional custom bucket name for frontend assets"
  default     = "front-itau-test-20260411"
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
