# uconia-api

A small .NET 10 minimal API, deployed to AWS ECS Fargate through the platform's golden path.

| Endpoint | What |
|---|---|
| `GET /health` | Health check for the ALB target group |
| `GET /api/info` | Service name, running version (commit SHA) and the task that answered |
| `GET /api/hello/{name}` | Example endpoint |
| `GET /api/db` | Connects to the service's RDS PostgreSQL (password injected from Secrets Manager) and returns its version |

## How it gets deployed

Push to `main` → `.github/workflows/deploy.yml` calls the platform's reusable pipeline
([uconia-golden-path/.github/workflows/ecs-deploy.yml](https://github.com/abhishekpatwa-ai/uconia-golden-path/blob/main/.github/workflows/ecs-deploy.yml)):

1. Log in to AWS with OIDC (no access keys), as role `uconia-api-github-deploy`
2. Build the Docker image (multi-stage), tag = commit SHA
3. Push to ECR, then block the deploy if the scan finds CRITICAL vulnerabilities
4. Register a new task definition revision and roll it out (circuit breaker rolls back on failure)

The infrastructure (ECR repo, ECS service, target group, ALB rule `/api/*`, roles) comes from
`modules/ecs-service` in [uconia-golden-path](https://github.com/abhishekpatwa-ai/uconia-golden-path), declared in `services/uconia-api.tf`.
