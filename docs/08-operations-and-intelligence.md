# Fase 5 — Operations & Intelligence

## Problema macro
> "Como garantir confiabilidade e escala?"

Este contexto nao gera receita diretamente, mas protege todas as fases anteriores: rastreabilidade de acoes, sincronizacao de dispositivos offline e visibilidade analitica para decisoes de negocio.

## Tabelas envolvidas
`report_config` · `audit_log` · `device` · `sync_status`

---

## Funcionalidades de nivel medio (features)

### 1. Auditoria
- Registro automatico de toda operacao de escrita (insert, update, delete) em qualquer tabela.
- Rastreabilidade: quem fez, o que mudou, quando.
- Detalhes da alteracao armazenados em jsonb para flexibilidade.

### 2. Gestao de dispositivos
- Cadastro de terminais/PDVs por empresa.
- Monitoramento de status online/offline.
- Registro de ultima sincronizacao.

### 3. Sincronizacao offline/online
- Controle por registro: qual tabela, qual registro, em qual dispositivo.
- Status de sincronizacao: pending, synced, failed.
- Suporte a operacao offline com reconciliacao posterior.

### 4. Relatorios configuraveis
- Templates de relatorio por empresa.
- Tipos: vendas, estoque, financeiro, fiscal, etc.
- Parametros configuraveis armazenados em jsonb.

---

## Funcionalidades de nivel baixo (operacionais)

### CRUD
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| Auto | AuditLog | Inserido via middleware/interceptor; imutavel |
| CRUD | Device | Por companyId; nome, status, ultima sync |
| CRUD | SyncStatus | Por deviceId + tabela + registro |
| CRUD | ReportConfig | Por companyId; nome, tipo, parametros |

### Endpoints / Casos de uso

**Auditoria**
- `GET /audit-logs?table=&recordId=&userId=&from=&to=` — consultar logs com filtros.
- `GET /audit-logs/:id` — detalhe de uma alteracao especifica.

**Dispositivos**
- `GET /devices` — listar dispositivos da empresa.
- `POST /devices` — registrar novo dispositivo.
- `PUT /devices/:id` — atualizar nome/configuracao.
- `DELETE /devices/:id` — desativar dispositivo.
- `POST /devices/:id/heartbeat` — atualizar isOnline e lastSyncAt.
- `GET /devices/:id/sync-status?status=pending` — registros pendentes de sync.

**Sincronizacao**
- `POST /sync/push` — enviar registros do dispositivo para o servidor.
- `POST /sync/pull` — solicitar registros atualizados desde lastSyncAt.
- `PATCH /sync-status/:id` — marcar registro como synced/failed.
- `GET /sync/conflicts?deviceId=` — listar conflitos para resolucao manual.

**Relatorios**
- `GET /reports` — listar templates de relatorio da empresa.
- `POST /reports` — criar template de relatorio.
- `PUT /reports/:id` — atualizar parametros.
- `POST /reports/:id/generate` — executar relatorio com parametros e retornar dados.
- `GET /reports/:id/preview` — pre-visualizar relatorio.

### Automacoes

**Auditoria automatica**
- Middleware/interceptor que captura toda operacao de escrita (insert/update/delete).
- Registra: tableName, recordId, action, changedBy (userId do contexto), changeDetails (diff).
- AuditLog e append-only — nunca atualizado ou deletado.

**Heartbeat de dispositivo**
- Endpoint chamado periodicamente pelo terminal.
- Atualiza `isOnline = true` e `lastSyncAt = now()`.
- Timeout configuravel: se nao receber heartbeat em X minutos, marcar `isOnline = false`.

**Ciclo de sync**
1. Dispositivo envia registros modificados localmente (push).
2. Servidor aplica alteracoes, detecta conflitos.
3. Servidor retorna registros atualizados desde ultima sync (pull).
4. Dispositivo confirma recebimento; SyncStatus atualizado para `synced`.

### Queries especializadas
- **Logs por periodo**: filtro por intervalo de datas com paginacao.
- **Alteracoes de um registro**: historico completo de um recordId especifico.
- **Dispositivos offline**: `WHERE isOnline = false AND lastSyncAt < now() - interval`.
- **Pendencias de sync**: count por dispositivo com status = pending.

### Regras de dominio
- AuditLog e imutavel — somente insercao, nunca edicao ou exclusao.
- SyncStatus com conflito (mesmo registro alterado no servidor e no dispositivo) deve ser resolvido manualmente ou por politica configuravel (last-write-wins, server-wins).
- Heartbeat nao deve criar AuditLog (evitar ruido).
- ReportConfig.parameters e schema-free para suportar qualquer tipo de relatorio futuro.
- Geracao de relatorio e operacao read-only — nunca altera dados.
