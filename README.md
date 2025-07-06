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
      "RESTART_INTERVAL_MINUTES": "5"
    }
  }
}
```

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
