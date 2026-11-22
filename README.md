# UpdatePod

> **⚠️ DO NOT USE IN PRODUCTION**

UpdatePod is a background service that watches one pod in a Kubernetes cluster. When the image tag the pod runs (for example `latest`) points to a new digest in the registry, it restarts the pod's Deployment or StatefulSet so the new image is pulled.

This is useful for picking up new images without changing the image tag. The watched container must use `imagePullPolicy: Always`.

---

## 📦 Docker Image

Pull from GitHub Container Registry (built by CI from `main`):

```bash
docker pull ghcr.io/anilkay/updatepod:latest
```

Pull from Docker Hub:

```bash
docker pull aanilkay/updatepod:latest
```

---

## ⚙️ Environment Variables

| Variable Name              | Required | Description                                                                                         | Default / Example         |
| -------------------------- | -------- | --------------------------------------------------------------------------------------------------- | ------------------------- |
| `POD_NAMESPACE`            | Yes      | Namespace of the pod                                                                                | `default`                 |
| `POD_NAME_PREFIX`          | Yes      | Prefix of the pod name to match                                                                     | `my-app`                  |
| `POD_CONTAINER_NAME`       | No       | Container to check inside the pod; the first container is used when unset                           | `web`                     |
| `RESTART_INTERVAL_MINUTES` | No       | Minutes between checks (1–71582)                                                                    | `10`                      |
| `IMAGE_OPERATIONS_TIMEOUT` | No       | Timeout in seconds for each registry lookup (1–4294967)                                             | `30`                      |
| `USE_HARBOR`               | No       | `true` to look up images in Harbor (or another registry with the Docker Registry v2 API) using the robot credentials below | `false`                   |
| `HARBOR_ROBOT_USER`        | No       | Harbor robot account name, sent with HTTP Basic auth when `USE_HARBOR=true`                         | `robot$yourproject`       |
| `HARBOR_ROBOT_TOKEN`       | No       | Harbor robot account token                                                                          | —                         |
| `DOCKER_HUB_TOKEN`         | No       | Docker Hub token for private repositories or higher rate limits                                     | —                         |

Values are trimmed, and numeric/boolean values are validated at startup: an invalid value stops the service immediately with a clear message.

---

## 🚀 What It Does

Every `RESTART_INTERVAL_MINUTES`:

1. Finds the first pod in `POD_NAMESPACE` whose name starts with `POD_NAME_PREFIX`.
2. Checks that the container's `imagePullPolicy` is `Always`.
3. Reads the digest of the image the container is running, and asks the registry for the current digest of the same image tag.
4. If the digests are the same, does nothing. If they differ, it finds the owner of the pod:

   * Pod → ReplicaSet → Deployment, or
   * Pod → StatefulSet

   and patches it with a restart annotation (the same thing `kubectl rollout restart` does):

   ```yaml
   spec:
     template:
       metadata:
         annotations:
           kubectl.kubernetes.io/restartedAt: <timestamp>
   ```

---

## 📋 Requirements and Limitations

* **`imagePullPolicy: Always`** is required on the watched container; otherwise the check fails.
* **Supported registries** are chosen from the image name as written in the pod spec:
  * **Docker Hub:** the image must include the `docker.io/` prefix, for example `docker.io/nginx:latest` or `docker.io/user/app:latest`. Short names such as `nginx:latest` are not recognized yet.
  * **quay.io:** public images, no credentials needed.
  * **Harbor and other Docker Registry v2 registries:** set `USE_HARBOR=true` and the robot credentials.
* **HTTPS only.** Registry lookups never fall back to plain HTTP, so credentials are never sent unencrypted. Registries that only serve plain HTTP are not supported.
* **One target per instance.** Each UpdatePod watches one pod prefix and one container. Choose a prefix that matches only the pods you mean: `my-app` also matches `my-app-worker`.
* **Errors stop the service.** If a check fails (pod not found, wrong pull policy, registry unreachable), the process currently exits and Kubernetes restarts the container. See `KNOWN_ISSUES.md` (H2).

---

## 🧪 Local Testing

`UpdatePod/Properties/launchSettings.json` is tracked in git, so keep only non-secret values there:

```json
"profiles": {
  "UpdatePod": {
    "commandName": "Project",
    "environmentVariables": {
      "POD_NAMESPACE": "default",
      "POD_NAME_PREFIX": "my-app",
      "POD_CONTAINER_NAME": "web",
      "RESTART_INTERVAL_MINUTES": "1"
    }
  }
}
```

Set credentials in your shell instead; `dotnet run` passes them to the app:

```powershell
$env:USE_HARBOR = "true"
$env:HARBOR_ROBOT_USER = 'robot$yourproject'
$env:HARBOR_ROBOT_TOKEN = "your-harbor-token"
dotnet run --project UpdatePod
```

Locally, UpdatePod uses your current kubeconfig context.

## How to Add Harbor Robot User and Token

1. **Create a Robot Account** in your Harbor project:
  - Go to your Harbor project.
  - Navigate to **Robot Accounts**.
  - Click **New Robot Account** and give it permission to pull (read) artifacts.
  - Copy the generated username (e.g., `robot$yourproject`) and token.

2. **Pass them to UpdatePod** as `HARBOR_ROBOT_USER` and `HARBOR_ROBOT_TOKEN`, together with `USE_HARBOR=true`. In Kubernetes, store them in a `Secret` (see the deployment example below); locally, set them in your shell.

> Keep robot credentials secure and do not commit them.

## How to get Docker Hub Token

A token is only needed for private repositories or higher rate limits.

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
5. **Pass the token to UpdatePod** as `DOCKER_HUB_TOKEN`. UpdatePod sends it as `Authorization: JWT <token>`.

> **Note:** Keep your token secure and do not commit it.

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
          image: ghcr.io/anilkay/updatepod:latest
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
            # Optional, for Harbor. Create the Secret first, for example:
            # kubectl create secret generic updatepod-harbor -n your-namespace \
            #   --from-literal=user='robot$yourproject' --from-literal=token='your-harbor-token'
            # - name: USE_HARBOR
            #   value: "true"
            # - name: HARBOR_ROBOT_USER
            #   valueFrom:
            #     secretKeyRef:
            #       name: updatepod-harbor
            #       key: user
            # - name: HARBOR_ROBOT_TOKEN
            #   valueFrom:
            #     secretKeyRef:
            #       name: updatepod-harbor
            #       key: token
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
    resources: ["deployments", "statefulsets"]
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

Error handling and isolation are still limited; see `KNOWN_ISSUES.md` for the open issues. Use at your own risk.
