{{/*
Expand the name of the chart.
*/}}
{{- define "mymarina.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "mymarina.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Component-scoped names
*/}}
{{- define "mymarina.api.name" -}}
{{- printf "%s-api" (include "mymarina.fullname" .) }}
{{- end }}

{{- define "mymarina.webui.name" -}}
{{- printf "%s-webui" (include "mymarina.fullname" .) }}
{{- end }}

{{- define "mymarina.redis.name" -}}
{{- printf "%s-redis" (include "mymarina.fullname" .) }}
{{- end }}

{{- define "mymarina.database.name" -}}
{{- printf "%s-database" (include "mymarina.fullname" .) }}
{{- end }}

{{- define "mymarina.database.credentialSecret" -}}
{{- printf "%s-pguser-%s" (include "mymarina.database.name" .) .Values.database.username }}
{{- end }}

{{- define "mymarina.redis.credentialSecret" -}}
{{- printf "%s-redis-auth" (include "mymarina.fullname" .) }}
{{- end }}

{{- define "mymarina.jwt.credentialSecret" -}}
{{- printf "%s-jwt" (include "mymarina.fullname" .) }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "mymarina.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "mymarina.labels" -}}
helm.sh/chart: {{ include "mymarina.chart" . }}
{{ include "mymarina.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Base selector labels (name + instance only — add component in each template)
*/}}
{{- define "mymarina.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mymarina.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Component selector labels
*/}}
{{- define "mymarina.api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mymarina.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: api
{{- end }}

{{- define "mymarina.webui.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mymarina.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: webui
{{- end }}

{{- define "mymarina.redis.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mymarina.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: redis
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "mymarina.initContainers.waitForDatabase" -}}
- name: wait-for-database
  image: postgres:{{ .Values.database.postgresVersion }}
  command:
    - /bin/sh
    - -c
    - |
      RETRIES=0
      MAX_RETRIES={{ div .Values.database.readinessTimeoutSeconds 5 }}
      until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER"; do
        RETRIES=$((RETRIES + 1))
        if [ "$RETRIES" -ge "$MAX_RETRIES" ]; then
          echo "Timed out waiting for PostgreSQL after {{ .Values.database.readinessTimeoutSeconds }}s"
          exit 1
        fi
        echo "Waiting for PostgreSQL at $DB_HOST:$DB_PORT... ($RETRIES/$MAX_RETRIES)"
        sleep 5
      done
  env:
    - name: DB_HOST
      valueFrom:
        secretKeyRef:
          name: {{ include "mymarina.database.credentialSecret" . }}
          key: host
    - name: DB_PORT
      valueFrom:
        secretKeyRef:
          name: {{ include "mymarina.database.credentialSecret" . }}
          key: port
    - name: DB_USER
      valueFrom:
        secretKeyRef:
          name: {{ include "mymarina.database.credentialSecret" . }}
          key: user
{{- end }}

{{- define "mymarina.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "mymarina.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}
