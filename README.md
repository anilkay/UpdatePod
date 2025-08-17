# UpdatePod

> **⚠️ DO NOT USE IN PRODUCTION**

UpdatePod is a background service that monitors a running pod in a Kubernetes cluster and automatically restarts its managing Deployment based on environment configuration.

This is useful for triggering image pull/update without replacing the image tag — especially when `imagePullPolicy: Always` is used.

---

## 📦 Docker Image

Pull from Docker Hub:

```bash
docker pull aanilkay/updatepod:latest
```

---

## ⚙️ Environment Variables

| Variable Name              | Description                                    | Example   |
| -------------------------- | ---------------------------------------------- | --------- |
| `POD_NAMESPACE`            | Namespace of the pod                           | `default` |
| `POD_NAME_PREFIX`          | Prefix of the pod name to match                | `my-app`  |
| `POD_CONTAINER_NAME`       | (Optional) Container name inside the pod       | `web`     |
| `RESTART_INTERVAL_MINUTES` | Interval in minutes for auto-check and restart | `10`      |

> All values are case-sensitive. If a pod with the given prefix is not found, nothing will happen.

---

## 🚀 What It Does

1. On start, reads environment variables.
2. Finds the first pod that starts with the provided prefix.
3. Follows the chain:

   * Pod → ReplicaSet → Deployment
4. Patches the Deployment to add a restart annotation:

   ```yaml
   spec:
     template:
       metadata:
         annotations:
           kubectl.kubernetes.io/restartedAt: <timestamp>
   ```
5. Waits for the given interval, then repeats.

---

## 🧪 Local Testing with launchSettings.json

```json
"profiles": {
  "UpdatePod": {
    "commandName": "Project",
    "environmentVariables": {
      "POD_NAMESPACE": "default",
      "POD_NAME_PREFIX": "my-app",
      "POD_CONTAINER_NAME": "web",
      "HARBOR_ROBOT_USER": "robot$yourproject",
      "HARBOR_ROBOT_TOKEN": "your-harbor-token",
      "DOCKER_HUB_TOKEN": "your-docker-hub-token"
    }
  }
}
```
## How to Add Harbor Robot User and Token

To use Harbor robot credentials, follow these steps:

1. **Create a Robot Account** in your Harbor project:
  - Go to your Harbor project.
  - Navigate to **Robot Accounts**.
  - Click **New Robot Account** and set permissions.
  - Copy the generated username (e.g., `robot$yourproject`) and token.

2. **Add to `launchSettings.json`**:
  - Set `HARBOR_ROBOT_USER` to the robot username.
  - Set `HARBOR_ROBOT_TOKEN` to the robot token.

**Example:**
```json
"environmentVariables": {
  "HARBOR_ROBOT_USER": "robot$yourproject",
  "HARBOR_ROBOT_TOKEN": "your-harbor-token"
}
```

> Keep robot credentials secure and do not share them publicly.

## How to get Docker Hub Token

To obtain a Docker Hub token for API authentication, follow these steps:

1. **Send a POST request** to the Docker Hub login endpoint:
  ```
  https://hub.docker.com/v2/users/login/
  ```
2. **Include your Docker Hub username and password** in the request body as JSON:
  ```json
  {
    "username": "your_dockerhub_username",
    "password": "your_dockerhub_password"
  }
  ```
3. **Example using `curl`:**
  ```sh
  curl -X POST -H "Content-Type: application/json" \
    -d '{"username": "your_dockerhub_username", "password": "your_dockerhub_password"}' \
    https://hub.docker.com/v2/users/login/
  ```
4. **The response will contain a token**:
  ```json
  {
    "token": "your_dockerhub_token"
  }
  ```
5. **Use this token** as a Bearer token in the `Authorization` header for subsequent Docker Hub API requests.

> **Note:** Keep your token secure and do not share it publicly.
---

## ☸️ Kubernetes Deployment Example

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: updatepod
  namespace: your-namespace
spec:
  replicas: 1
  selector:
    matchLabels:
      app: updatepod
  template:
    metadata:
      labels:
        app: updatepod
    spec:
      serviceAccountName: updatepod-sa
      containers:
        - name: updatepod
          image: aanilkay/updatepod:latest
          env:
            - name: POD_NAMESPACE
              valueFrom:
                fieldRef:
                  fieldPath: metadata.namespace
            - name: POD_NAME_PREFIX
              value: my-app
            - name: POD_CONTAINER_NAME
              value: web
            - name: RESTART_INTERVAL_MINUTES
              value: "10"
```

---

## 🔐 Minimal Required RBAC Permissions

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: updatepod-sa
  namespace: your-namespace
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: updatepod-role
  namespace: your-namespace
rules:
  - apiGroups: [""]
    resources: ["pods"]
    verbs: ["get", "list"]
  - apiGroups: ["apps"]
    resources: ["replicasets"]
    verbs: ["get"]
  - apiGroups: ["apps"]
    resources: ["deployments"]
    verbs: ["get", "patch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: updatepod-binding
  namespace: your-namespace
subjects:
  - kind: ServiceAccount
    name: updatepod-sa
    namespace: your-namespace
roleRef:
  kind: Role
  name: updatepod-role
  apiGroup: rbac.authorization.k8s.io
```

---

## ❗ Disclaimer

This tool is intended for internal development/testing purposes only.

**Do not use in production environments.**

It does not include any validation, security handling, or error isolation. Use at your own risk.
