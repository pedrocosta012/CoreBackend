# Fase 1 — Identity & Tenancy

## Problema macro
> "Quem pode operar o sistema e sob qual empresa?"

Sem este contexto nenhum outro funciona. Ele estabelece isolamento multi-tenant, autenticacao e controle de acesso — pré-requisito para qualquer operacao de receita.

## Tabelas envolvidas
`user` · `company` · `address` · `company_address` · `company_role` · `user_config` · `company_config`

---

## Funcionalidades de nivel medio (features)

### 1. Autenticacao
- Registro de usuario (username, email, phone, password).
- Login com geracao de token (JWT).
- Recuperacao/redefinicao de senha.
- Logout e invalidacao de sessao.

### 2. Multi-tenancy
- Cada empresa (company) opera como tenant isolado.
- Dados de qualquer contexto sao sempre filtrados por `companyId`.
- Suporte a matriz (headOffice) e filiais (branch).

### 3. Controle de acesso (RBAC)
- Associacao usuario ↔ empresa com niveis de acesso por dominio.
- Dominios de permissao: `sales`, `inventory`, `admin`.
- Niveis por dominio: `block`, `view`, `edit`.
- Um usuario pode ter papeis distintos em empresas diferentes.

### 4. Gestao de empresas
- Cadastro de empresa com tipo (matriz/filial) e documento fiscal (taxId).
- Vinculacao de enderecos a empresa por tipo (billing, shipping, location).

### 5. Configuracoes
- Preferencias por usuario (user_config.preferences — jsonb).
- Configuracoes por empresa (company_config.settings — jsonb).

---

## Funcionalidades de nivel baixo (operacionais)

### CRUD
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | User | username unico; soft-delete via deletedAt |
| CRUD | Company | officeType obrigatorio; soft-delete via deletedAt |
| CRUD | Address | Reutilizavel por company e customer |
| CRUD | CompanyAddress | Tabela associativa companyId + addressId + type |
| CRUD | CompanyRole | Chave composta userId + companyId; niveis por dominio |
| CRUD | UserConfig | Relacao 1:1 com user |
| CRUD | CompanyConfig | Relacao 1:1 com company |

### Endpoints / Casos de uso
- `POST /auth/register` — criar usuario.
- `POST /auth/login` — autenticar e retornar token.
- `POST /auth/refresh` — renovar token.
- `POST /auth/forgot-password` — iniciar fluxo de recuperacao.
- `GET /me` — dados do usuario autenticado.
- `GET /me/companies` — empresas vinculadas ao usuario com respectivos papeis.
- `PUT /me/config` — atualizar preferencias do usuario.
- `POST /companies` — criar empresa.
- `PUT /companies/:id` — atualizar empresa.
- `POST /companies/:id/addresses` — vincular endereco.
- `POST /companies/:id/roles` — convidar/associar usuario com permissoes.
- `PUT /companies/:id/roles/:userId` — alterar niveis de acesso.
- `DELETE /companies/:id/roles/:userId` — revogar acesso (soft-delete).
- `PUT /companies/:id/config` — atualizar configuracoes da empresa.

### Middlewares obrigatorios
- **AuthMiddleware** — valida token JWT em toda requisicao protegida.
- **TenantMiddleware** — extrai `companyId` do contexto e injeta no pipeline; rejeita acesso cruzado.
- **AccessMiddleware** — valida nivel de acesso do usuario no dominio da rota (sales/inventory/admin).

### Regras de dominio
- Um usuario sem `company_role` ativo nao pode operar em nenhuma empresa.
- `admin.edit` e necessario para alterar roles de outros usuarios.
- Soft-delete em `company_role` revoga acesso sem perder historico.
- `company_config` e `user_config` devem ser criados automaticamente junto com a respectiva entidade pai.
