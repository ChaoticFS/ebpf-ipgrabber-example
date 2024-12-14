Last updated: 14/12/24

Deploying to kubernetes via kubectl, minikube and docker desktop

These commands need to be run in order to deploy it to k8s

Open Docker Desktop

minikube start

minikube dashboard # Will lock your terminal, start an extra

# I'd recommend opening like 4 terminals and doing the following command in them all:
cd "path/to/your/repo"

helm upgrade --install --values Build/monitoring-deployment.yaml loki grafana/loki-stack -n grafana-loki --create-namespace

helm upgrade --install --values Build/database-deployment.yaml rqlite rqlite/rqlite --create-namespace

# ONLY RUN THESE STEPS IF YOU WANT TO REINDEX DB
# ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
minikube mount "path/to/searchCase/seData/seData copy/medium:/mnt/data"
kubectl apply -f Build/database-initialization.yaml
# ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

kubectl apply -f Build/config-values.yaml

kubectl apply -f Build/cache-deployment.yaml

kubectl apply -f Build/searchapi-deployment.yaml

kubectl get pods

kubectl port-forward <navn-på-searchapi-pod> 5262:5262

kubectl apply -f Build/websearch-deployment.yaml

minikube service websearch-service