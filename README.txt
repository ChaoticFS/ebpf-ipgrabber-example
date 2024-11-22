Version 1: 23-8-2024

A seachengine that consist of an indexer and a search program.

The indexer will crawl a folder (in depth) and create a reverse index
in a database. It will only index text files with .txt as extension.

The search program is a console program that offers a query-based search
in the reverse index. It is in the ConsoleSearch project.

The class library Shared contains classes that are used by the indexer
and the ConsoleSearch. It contains:

- Paths containing static paths for 1) files to index and, 2) a path for
  the database
- BEDocument (BE for Business Entity) - a class representing a document.

The project Renamer is a console program used to rename all files in a
folder. Current version will rename all files with no extension to have
.txt as extension.


Version 2: 22-11-2024

Deploying to kubernetes via kubectl, minikube and docker desktop

These commands need to be run in order to deploy it to k8s

cd "path/to/repo"

minikube start

minikube dashboard # Will lock your terminal, start an extra

minikube mount "path/to/searchCase/seData/seData copy/medium:/mnt/data" # Will lock your terminal, start an extra

# If you've already renamed/indexed go into the config-values.yaml and change the SKIP_PROCESSING var to true
kubectl apply -f Build/config-values.yaml

kubectl apply -f Build/searchapi-deployment.yaml

kubectl get pods

kubectl port-forward <navn-på-searchapi-pod> 5262:5262

kubectl apply -f Build/websearch-deployment.yaml