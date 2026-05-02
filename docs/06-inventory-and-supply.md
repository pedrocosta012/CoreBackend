# Fase 3 — Inventory & Supply

## Problema macro
> "Como controlar custo e disponibilidade?"

Protege a margem de lucro ao rastrear cada unidade de insumo: onde esta, quanto tem, quanto custa. Garante que vendas nao acontecam sem estoque e que compras reponham o necessario.

## Tabelas envolvidas
`stock_location` · `inventory_item` · `inventory_level` · `product_composition` · `inventory_transaction` · `supplier` · `purchase` · `purchase_item`

---

## Funcionalidades de nivel medio (features)

### 1. Locais de estoque
- Multiplos locais por empresa (loja, deposito, cozinha, bar).
- Cada local pode ter endereco vinculado.
- Tipificacao livre (store, warehouse, kitchen, bar).

### 2. Itens de inventario
- Insumos e materias-primas como entidades independentes de produtos.
- Unidade de medida (ml, g, un) e custo unitario.
- Um item de inventario pode compor multiplos produtos (ficha tecnica).

### 3. Niveis de estoque
- Quantidade em maos (onHand) por item + local.
- Quantidade reservada (reserved) para pedidos em andamento.
- Ponto de reposicao (reorderPoint) para alertas automaticos.
- Estoque disponivel = onHand - reserved.

### 4. Ficha tecnica (composicao)
- Relacao produto ↔ item de inventario com quantidade por unidade vendida.
- Permite calculo automatico de baixa: ao vender 1 produto, deduzir N insumos.
- Base para custeio de produto (CMV).

### 5. Movimentacoes de estoque
- Registro de toda entrada e saida com tipo explicito.
- Tipos: Purchase (compra), Sale (venda), Production (producao), Adjustment (ajuste), Transfer (transferencia).
- Referencia generica (referenceTable + referenceId) para rastreabilidade cruzada.

### 6. Fornecedores
- Cadastro de fornecedores por empresa com documento, contato e endereco.

### 7. Compras
- Registro de compras por fornecedor com total e data.
- Itens de compra referenciam itens de inventario com quantidade e preco.
- Cada compra gera movimentacoes de entrada automaticamente.

---

## Funcionalidades de nivel baixo (operacionais)

### CRUD
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | StockLocation | Por companyId; tipo e endereco opcional |
| CRUD | InventoryItem | Por companyId; unidade e custo |
| CRUD | InventoryLevel | Par (inventoryItemId, locationId) unico |
| CRUD | ProductComposition | Par (productId, inventoryItemId); quantityPerUnit |
| Auto | InventoryTransaction | Inserido por eventos de dominio; imutavel |
| CRUD | Supplier | Por companyId; documento, contato, endereco |
| CRUD | Purchase | Por companyId + supplierId |
| CRUD | PurchaseItem | Por purchaseId + inventoryItemId |

### Endpoints / Casos de uso

**Locais de estoque**
- `GET /stock-locations` — listar locais da empresa.
- `POST /stock-locations` — criar local.
- `PUT /stock-locations/:id` — atualizar local.

**Itens de inventario**
- `GET /inventory-items` — listar insumos da empresa.
- `POST /inventory-items` — criar insumo.
- `PUT /inventory-items/:id` — atualizar insumo (nome, custo, unidade).
- `GET /inventory-items/:id/levels` — niveis de estoque por local.

**Niveis e movimentacoes**
- `GET /inventory-levels?locationId=&belowReorder=true` — consultar niveis com filtros.
- `POST /inventory-transactions` — registrar movimentacao manual (ajuste, transferencia).
- `GET /inventory-transactions?itemId=&locationId=&type=&from=&to=` — historico de movimentacoes.

**Ficha tecnica**
- `GET /products/:id/composition` — listar insumos do produto com quantidades.
- `POST /products/:id/composition` — vincular insumo ao produto.
- `PUT /products/:id/composition/:inventoryItemId` — atualizar quantidade.
- `DELETE /products/:id/composition/:inventoryItemId` — remover insumo.

**Fornecedores**
- `GET /suppliers` — listar fornecedores da empresa.
- `POST /suppliers` — cadastrar fornecedor.
- `PUT /suppliers/:id` — atualizar fornecedor.

**Compras**
- `POST /purchases` — registrar compra com itens.
- `GET /purchases` — listar compras com filtro por fornecedor/data.
- `GET /purchases/:id` — compra completa com itens.

### Automacoes (eventos de dominio)
- **Ao confirmar compra**: para cada PurchaseItem, criar InventoryTransaction tipo `Purchase` (+quantidade) e atualizar InventoryLevel.onHand.
- **Ao confirmar venda**: para cada SaleItem, consultar ProductComposition, criar InventoryTransaction tipo `Sale` (-quantidade * composicao) e atualizar InventoryLevel.onHand.
- **Ao reservar pedido**: incrementar InventoryLevel.reserved; ao cancelar, decrementar.
- **Ao registrar transferencia**: criar duas InventoryTransactions (saida do local A, entrada no local B).

### Queries especializadas
- **Estoque disponivel**: `onHand - reserved` por item e local.
- **Itens abaixo do ponto de reposicao**: `WHERE onHand <= reorderPoint`.
- **CMV (Custo de Mercadoria Vendida)**: soma de (composicao.quantityPerUnit * inventoryItem.cost) por produto.
- **Movimentacoes por periodo**: filtro por tipo, item, local e intervalo de datas.

### Regras de dominio
- InventoryTransaction e imutavel — correcoes sao feitas via novo registro tipo `Adjustment`.
- Estoque disponivel nao pode ser negativo (validar antes de confirmar venda).
- Ponto de reposicao e informativo; nao bloqueia operacoes automaticamente.
- ProductComposition.quantityPerUnit deve ser > 0.
- Ao alterar custo de um InventoryItem, nao retroagir — vale para movimentacoes futuras.
