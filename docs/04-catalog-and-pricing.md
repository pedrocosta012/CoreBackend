# Fase 2A — Catalog & Pricing

## Problema macro
> "O que e vendido e por quanto?"

Define tudo que pode ser comercializado — produtos, servicos, variantes, opcionais e combos — e estabelece a estrategia de precificacao por canal com historico auditavel.

## Tabelas envolvidas
`category` · `product` · `product_variant` · `product_category` · `product_attribute` · `option_group` · `option_item` · `product_option_group` · `bundle_item` · `price_list` · `product_variant_price` · `price_change_log`

---

## Funcionalidades de nivel medio (features)

### 1. Catalogo de categorias
- Categorias hierarquicas (parentId) por empresa.
- Um produto pode pertencer a multiplas categorias (N:N).

### 2. Gestao de produtos
- Produto como entidade abstrata (nome, descricao, flag isService).
- Variantes como unidades vendaveis (SKU, barcode, preco base, custo, peso, dimensoes).
- Ativacao/desativacao de variantes sem exclusao.

### 3. Atributos customizaveis
- Pares chave-valor livres por produto (cor, tamanho, sabor, etc.).
- Sem schema rigido — flexibilidade para qualquer segmento.

### 4. Opcionais e adicionais
- Grupos de opcoes vinculados a produtos (ex: "Ponto da carne", "Extras").
- Regras de selecao: minimo, maximo, obrigatoriedade.
- Cada item opcional pode ter delta de preco (+R$2, -R$1, etc.).

### 5. Combos e kits (bundles)
- Uma variante "pai" composta por variantes "filhas" com quantidade definida.
- Permite venda agrupada com preco proprio.

### 6. Listas de preco
- Multiplas listas por empresa (Padrao, iFood, Atacado, Loja Fisica, etc.).
- Opcional: vincular lista a um canal de venda especifico.
- Uma lista marcada como padrao (isDefault).

### 7. Versionamento de precos
- Preco por variante + lista com periodo de vigencia (validFrom / validTo).
- Historico completo: quem alterou, quando, motivo.
- Preco vigente = registro com validFrom <= agora e validTo nulo ou >= agora.

### 8. Auditoria de precos
- Registro automatico em price_change_log a cada alteracao.
- Campos: preco antigo, preco novo, quem alterou, motivo.

---

## Funcionalidades de nivel baixo (operacionais)

### CRUD
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | Category | Hierarquica (parentId); filtrada por companyId |
| CRUD | Product | Filtrado por companyId; flag isService |
| CRUD | ProductVariant | SKU unico; barcode opcional; preco base e custo |
| CRUD | ProductCategory | Associacao N:N entre produto e categoria |
| CRUD | ProductAttribute | Pares chave-valor por produto |
| CRUD | OptionGroup | minSelect, maxSelect, required por empresa |
| CRUD | OptionItem | priceDelta; isDefault dentro do grupo |
| CRUD | ProductOptionGroup | Associacao N:N entre produto e grupo de opcoes |
| CRUD | BundleItem | parentVariantId → childVariantId + quantity |
| CRUD | PriceList | Por empresa; canal opcional; flag isDefault |
| CRUD | ProductVariantPrice | Preco versionado com vigencia |
| Auto | PriceChangeLog | Inserido automaticamente ao criar/atualizar ProductVariantPrice |

### Endpoints / Casos de uso
- `GET /categories` — arvore de categorias da empresa.
- `POST /categories` — criar categoria (com parentId opcional).
- `GET /products` — listar produtos com filtros (categoria, ativo, servico).
- `GET /products/:id` — produto completo com variantes, atributos, opcoes.
- `POST /products` — criar produto com variantes iniciais.
- `PUT /products/:id` — atualizar produto.
- `POST /products/:id/variants` — adicionar variante.
- `PUT /products/:id/variants/:variantId` — atualizar variante.
- `PATCH /products/:id/variants/:variantId/active` — ativar/desativar variante.
- `POST /products/:id/attributes` — adicionar atributo.
- `POST /products/:id/option-groups` — vincular grupo de opcoes.
- `POST /products/:id/variants/:variantId/bundle-items` — definir composicao do combo.
- `GET /price-lists` — listas de preco da empresa.
- `POST /price-lists` — criar lista de preco.
- `POST /price-lists/:id/prices` — definir preco de variante na lista (com vigencia).
- `GET /price-lists/:id/prices?variantId=&date=` — consultar preco vigente.
- `GET /products/:id/variants/:variantId/price-history` — historico de alteracoes.

### Queries especializadas
- **Preco vigente**: buscar `product_variant_price` onde `validFrom <= NOW()` e (`validTo IS NULL` ou `validTo >= NOW()`), ordenado por `validFrom DESC`, limitado a 1.
- **Arvore de categorias**: query recursiva por `parentId` para montar hierarquia.
- **Produtos por categoria**: join via `product_category` com suporte a categorias filhas.
- **Composicao de bundle**: listar filhos com nome, SKU e quantidade.

### Regras de dominio
- Apenas uma lista de preco pode ser `isDefault = true` por empresa.
- Ao criar um novo ProductVariantPrice, o registro anterior deve ter seu `validTo` preenchido automaticamente.
- PriceChangeLog e imutavel — somente insercao.
- SKU deve ser unico globalmente; barcode unico quando informado.
- OptionGroup.maxSelect = 0 significa sem limite de selecao.
- BundleItem nao pode referenciar a propria variante pai (prevenir recursao).
