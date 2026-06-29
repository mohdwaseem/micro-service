// MicroServiceDemo CI/CD Pipeline
//
// Stages:
//   1. Checkout   — fetch source from Gitea
//   2. Build      — dotnet restore + build (Release)
//   3. Unit Tests — xUnit tests excluding E2E; publishes JUnit results
//   4. DB Migrate — DbUp migrator creates/upgrades all three SQL Server databases
//   5. Docker Build — rebuild all service images
//   6. Deploy       — docker compose up -d (rolling restart)
//
// Prerequisites in Jenkins → Manage Credentials:
//   MSDEMO_SA_PASSWORD   (Secret text) — SQL Server SA password
//   MSDEMO_JWT_SECRET    (Secret text) — JWT signing secret
//   MSDEMO_RABBITMQ_USER (Secret text) — RabbitMQ username
//   MSDEMO_RABBITMQ_PASS (Secret text) — RabbitMQ password

pipeline {
    agent any

    options {
        timestamps()
        timeout(time: 30, unit: 'MINUTES')
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
        ansiColor('xterm')
    }

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO               = '1'

        // Injected from Jenkins credential store — never hardcoded here
        SA_PASSWORD   = credentials('MSDEMO_SA_PASSWORD')
        JWT_SECRET    = credentials('MSDEMO_JWT_SECRET')
        RABBITMQ_USER = credentials('MSDEMO_RABBITMQ_USER')
        RABBITMQ_PASS = credentials('MSDEMO_RABBITMQ_PASS')
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
                script {
                    def shortCommit = env.GIT_COMMIT?.take(8) ?: 'unknown'
                    echo "Branch: ${env.GIT_BRANCH ?: 'unknown'} | Commit: ${shortCommit}"
                }
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet restore MicroServiceDemo.slnx'
                sh 'dotnet build MicroServiceDemo.slnx --configuration Release --no-restore'
            }
        }

        stage('Unit Tests') {
            steps {
                sh '''
                    dotnet test MicroServiceDemo.slnx \
                        --configuration Release \
                        --no-build \
                        --filter "Category!=E2E" \
                        --logger "trx;LogFileName=results.trx" \
                        --results-directory TestResults
                '''
            }
            post {
                always {
                    // Publish test results even if tests fail
                    junit allowEmptyResults: true,
                          testResults: 'TestResults/**/*.trx'
                }
            }
        }

        stage('DB Migrate') {
            // Runs DbUp against the SQL Server exposed on the Docker host.
            // host.docker.internal resolves to the host machine from inside a container.
            steps {
                sh '''
                    dotnet run \
                        --project src/DatabaseMigrator \
                        --configuration Release \
                        -- \
                        --user    "Server=host.docker.internal,1433;Database=UserServiceDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True" \
                        --product "Server=host.docker.internal,1433;Database=ProductServiceDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True" \
                        --order   "Server=host.docker.internal,1433;Database=OrderServiceDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"
                '''
            }
        }

        stage('Docker Build') {
            steps {
                sh 'docker compose build'
            }
        }

        stage('Deploy') {
            steps {
                sh '''
                    SA_PASSWORD="${SA_PASSWORD}" \
                    JWT_SECRET="${JWT_SECRET}" \
                    RABBITMQ_USER="${RABBITMQ_USER}" \
                    RABBITMQ_PASS="${RABBITMQ_PASS}" \
                    docker compose up -d --remove-orphans
                '''
            }
        }

    }

    post {
        success {
            emailext(
                subject: "BUILD PASSED: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: """Pipeline succeeded.

Job:    ${env.JOB_NAME}
Build:  #${env.BUILD_NUMBER}
Branch: ${env.GIT_BRANCH ?: 'unknown'}

View: ${env.BUILD_URL}""",
                to: 'mohdwaseem488@gmail.com'
            )
        }
        failure {
            emailext(
                subject: "BUILD FAILED: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: """Pipeline failed.

Job:    ${env.JOB_NAME}
Build:  #${env.BUILD_NUMBER}
Branch: ${env.GIT_BRANCH ?: 'unknown'}
Stage:  ${env.STAGE_NAME ?: 'unknown'}

View: ${env.BUILD_URL}""",
                to: 'mohdwaseem488@gmail.com'
            )
        }
        always {
            cleanWs()
        }
    }
}
