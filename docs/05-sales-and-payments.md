# Fase 2B — Sales & Payments

## Problema macro
> "Como capturar receita?"

Este contexto cobre o ciclo completo desde o cliente ate o dinheiro no caixa: cadastro de clientes, pedidos, conversao em venda, recebimento de pagamentos, controle de caixa, fiscalidade e promocoes.

## Tabelas envolvidas
`customer` · `customer_address` · `company_customer` · `order` · `order_item` · `order_item_option` · `order_item_price_snapshot` · `sale` · `sale_item` · `payment` · `cash_book` · `cash_book_sale` · `tax_config` · `promotion` · `promotion_category` · `product_promotion` · `promotion_customer` · `promotion_redemption`

---

## Funcionalidades de nivel medio (features)

### 1. Gestao de clientes
- Cadastro global de clientes (nome, documento, email, phone).
- Enderecos multiplos por cliente com marcacao de padrao.
- Vinculo cliente ↔ empresa com dados especificos: pontos de fidelidade, limite de credito, observacoes.

### 2. Ciclo de vida do pedido
- Abertura de pedido com canal de venda e tipo de fulfillment.
- Status: `open` → `held` → `pending_payment` → `paid` → `fulfilled` → `completed`.
- Cancelamento e reembolso como transicoes explicitas.
- Vinculo opcional com mesa (A&B) e referencia externa (e-commerce).

### 3. Itens do pedido
- Cada item referencia uma variante com quantidade, preco unitario e desconto.
- Opcionais aplicados por item (order_item_option) com delta de preco.
- Snapshot do preco no momento da confirmacao para auditoria.
- Calculo automatico de totais: subtotal, desconto, imposto, taxa de servico, total.

### 4. Conversao pedido → venda
- Venda criada a partir de um pedido pago.
- Registro de numero de nota fiscal (invoiceNumber).
- Itens da venda espelham os itens do pedido no momento da conversao.

### 5. Pagamentos
- Multiplos pagamentos por venda (split payment).
- Tipos: cash, card, pix, boleto, transfer, voucher, credit, debit.
- Dados de adquirente, codigo de autorizacao e parcelas.

### 6. Controle de caixa
- Abertura de caixa com saldo inicial por operador.
- Vinculo de vendas ao caixa aberto.
- Fechamento com saldo final calculado.
- Status do caixa: open / closed.

### 7. Configuracao fiscal
- Regras tributarias por produto e empresa.
- Tipos: ICMS, IPI, PIS, COFINS, ISS.
- Campos: aliquota (rate), CST, CFOP, NCM.

### 8. Promocoes e descontos
- Promocoes por percentual ou valor fixo.
- Escopo: geral, por categoria, por produto, por cliente.
- Controles: valor minimo de pedido, limite total de usos, limite por cliente.
- Vigencia: data de inicio e fim, flag ativo/inativo.
- Registro de cada resgate com desconto aplicado.

---

## Funcionalidades de nivel baixo (operacionais)

### CRUD
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | Customer | Documento opcional; global (nao pertence a uma empresa) |
| CRUD | CustomerAddress | Associativa customerId + addressId; flag isDefault |
| CRUD | CompanyCustomer | companyId + customerId unico; loyalty, credito, nota |
| CRUD | Order | Canal, fulfillment, status; filtrado por companyId |
| CRUD | OrderItem | Referencia variantId; preco e desconto por item |
| CRUD | OrderItemOption | optionItemId + priceDelta por item do pedido |
| Auto | OrderItemPriceSnapshot | Criado ao confirmar item; imutavel |
| CRUD | Sale | Criada a partir de Order; invoiceNumber |
| CRUD | SaleItem | Espelha OrderItem no momento da conversao |
| CRUD | Payment | Multiplos por sale; tipo, valor, dados do adquirente |
| CRUD | CashBook | Abertura/fechamento por usuario + empresa |
| Auto | CashBookSale | Vinculo automatico sale ↔ caixa aberto |
| CRUD | TaxConfig | Por companyId + productId; tipo, aliquota, codigos |
| CRUD | Promotion | Regras gerais da promocao |
| CRUD | PromotionCategory | Escopo por categoria |
| CRUD | ProductPromotion | Escopo por produto |
| CRUD | PromotionCustomer | Escopo por cliente com limite individual |
| Auto | PromotionRedemption | Registro de uso; promotionId + saleId unico |

### Endpoints / Casos de uso

**Clientes**
- `GET /customers` — listar clientes com busca por nome/documento.
- `POST /customers` — cadastrar cliente.
- `POST /customers/:id/addresses` — adicionar endereco.
- `GET /companies/:companyId/customers` — clientes da empresa com dados de fidelidade.
- `POST /companies/:companyId/customers` — vincular cliente a empresa.
- `PUT /companies/:companyId/customers/:customerId` — atualizar loyalty/credito/nota.

**Pedidos**
- `POST /orders` — abrir pedido (canal, fulfillment, cliente opcional, mesa opcional).
- `GET /orders` — listar pedidos com filtro por status/canal/data.
- `GET /orders/:id` — pedido completo com itens, opcoes e totais.
- `POST /orders/:id/items` — adicionar item ao pedido.
- `PUT /orders/:id/items/:itemId` — alterar quantidade/desconto.
- `DELETE /orders/:id/items/:itemId` — remover item.
- `POST /orders/:id/items/:itemId/options` — aplicar opcional.
- `PATCH /orders/:id/status` — transicionar status (hold, cancel, refund).
- `POST /orders/:id/checkout` — finalizar pedido → gerar venda.

**Vendas e pagamentos**
- `GET /sales` — listar vendas com filtro por data/operador/caixa.
- `GET /sales/:id` — venda completa com itens e pagamentos.
- `POST /sales/:id/payments` — registrar pagamento.
- `GET /sales/:id/payments` — listar pagamentos da venda.

**Caixa**
- `POST /cash-books/open` — abrir caixa com saldo inicial.
- `POST /cash-books/:id/close` — fechar caixa; calcular saldo final.
- `GET /cash-books/:id` — resumo do caixa com vendas vinculadas.
- `GET /cash-books` — historico de caixas do operador.

**Fiscal**
- `GET /tax-configs` — configuracoes fiscais da empresa.
- `POST /tax-configs` — criar regra fiscal para produto.
- `PUT /tax-configs/:id` — atualizar regra.

**Promocoes**
- `GET /promotions` — listar promocoes da empresa.
- `POST /promotions` — criar promocao.
- `PUT /promotions/:id` — atualizar regras.
- `POST /promotions/:id/categories` — vincular categorias.
- `POST /promotions/:id/products` — vincular produtos.
- `POST /promotions/:id/customers` — vincular clientes com limite.
- `POST /promotions/validate` — validar codigo/elegibilidade para pedido.
- `GET /promotions/:id/redemptions` — historico de resgates.

### Calculos automaticos
- **Subtotal do pedido**: soma de (item.price * item.quantity) + opcoes aplicadas.
- **Desconto**: aplicacao de promocao validada ou desconto manual por item.
- **Imposto**: soma de impostos aplicaveis por item conforme tax_config.
- **Taxa de servico**: percentual configuravel sobre subtotal (ex: 10% A&B).
- **Total**: subtotal - desconto + imposto + taxa de servico.

### Regras de dominio
- Pedido so pode transicionar para `paid` se soma dos pagamentos >= total.
- Venda so e criada a partir de pedido com status `paid`.
- Pagamento so pode ser registrado em venda vinculada a caixa aberto.
- CashBook.closingBalance = openingBalance + soma(payments.cash) do periodo.
- Promocao so e aplicavel se: ativa, dentro da vigencia, dentro dos limites de uso, e pedido atende valor minimo.
- PromotionRedemption: par (promotionId, saleId) e unico — mesmo cupom nao pode ser aplicado duas vezes na mesma venda.
- OrderItemPriceSnapshot e imutavel — registra o preco exato no momento da captura.
