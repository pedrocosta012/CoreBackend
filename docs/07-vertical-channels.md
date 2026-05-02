# Fase 4 — Vertical Channels

## Problema macro
> "Como expandir para novos mercados?"

Cada sub-contexto abaixo desbloqueia um segmento de mercado adicional, reutilizando o core de catalogo, pedidos e pagamentos. Sao modulos independentes entre si — cada um pode ser ativado isoladamente conforme demanda do cliente.

---

## 4A — Food & Beverage (A&B)

### Tabelas envolvidas
`dining_area` · `dining_table` · `prep_station` · `kitchen_ticket`

### Funcionalidades de nivel medio
- Gestao de areas e mesas do estabelecimento.
- Vinculo de pedido a mesa (Order.tableId).
- KDS (Kitchen Display System): tickets de preparo por estacao.
- Fluxo: pedido → ticket na cozinha/bar → preparo → entrega na mesa.
- Controle de capacidade e status de mesas.

### Funcionalidades de nivel baixo

**CRUD**
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | DiningArea | Por companyId; agrupador logico de mesas |
| CRUD | DiningTable | Por areaId; nome, assentos, ativo/inativo |
| CRUD | PrepStation | Por companyId; cozinha, bar, confeitaria, etc. |
| Auto | KitchenTicket | Criado ao adicionar item a pedido de mesa |

**Endpoints**
- `GET /dining-areas` — listar areas com mesas.
- `POST /dining-areas` — criar area.
- `POST /dining-areas/:id/tables` — adicionar mesa.
- `PUT /dining-tables/:id` — atualizar mesa (nome, assentos, ativo).
- `GET /dining-tables?available=true` — mesas disponiveis.
- `GET /prep-stations` — listar estacoes de preparo.
- `POST /prep-stations` — criar estacao.
- `GET /prep-stations/:id/tickets?status=queued` — tickets pendentes da estacao.
- `PATCH /kitchen-tickets/:id/status` — transicionar: queued → in_progress → done.

**Automacoes**
- Ao adicionar OrderItem a pedido com tableId, criar KitchenTicket na estacao correspondente ao produto.
- Relacao produto → estacao pode ser via configuracao (ex: atributo ou categoria).

**Regras de dominio**
- Mesa so pode ter um pedido ativo (status != completed/cancelled) por vez.
- KitchenTicket so e criado para itens de produto (nao servicos).
- Transicao de status do ticket e unidirecional: queued → in_progress → done.

---

## 4B — E-commerce & Delivery

### Tabelas envolvidas
`shipment` · `delivery_order`

### Funcionalidades de nivel medio
- Envio e rastreamento de pedidos fisicos.
- Integracao com entregadores e marketplaces (iFood, Rappi, UberEats).
- Controle de custo de frete.
- Referencia externa para conciliacao com plataformas terceiras.

### Funcionalidades de nivel baixo

**CRUD**
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | Shipment | Por saleId; transportadora, rastreio, custo |
| CRUD | DeliveryOrder | Por orderId; provedor, ETA, ref externa |

**Endpoints**
- `POST /sales/:saleId/shipments` — registrar envio.
- `GET /shipments/:id` — detalhes do envio com rastreio.
- `PATCH /shipments/:id/status` — atualizar status: pending → shipped → delivered → returned.
- `POST /orders/:orderId/delivery` — criar pedido de delivery.
- `GET /delivery-orders?provider=&status=` — listar entregas com filtros.

**Automacoes**
- Ao criar Shipment, preencher shippedAt automaticamente se status = shipped.
- Ao marcar como delivered, preencher deliveredAt.

**Regras de dominio**
- Shipment so pode ser criado para venda com fulfillment = ship.
- DeliveryOrder so para pedido com fulfillment = delivery.
- Status do shipment e unidirecional (exceto returned como caminho alternativo).

---

## 4C — Services & Appointments

### Tabelas envolvidas
`service_definition` · `staff_skill` · `appointment`

### Funcionalidades de nivel medio
- Catalogo de servicos com duracao e preco base.
- Vinculo servico ↔ produto para faturamento via catalogo existente.
- Habilidades do staff: quem pode executar cada servico.
- Agendamento com horario, profissional e cliente.
- Fluxo de status: pending → confirmed → checked_in → checked_out → cancelled/no_show.
- Geracao de pedido a partir do agendamento.

### Funcionalidades de nivel baixo

**CRUD**
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | ServiceDefinition | Por companyId; duracao, preco, productId opcional |
| CRUD | StaffSkill | Par userId + serviceId |
| CRUD | Appointment | Por companyId; vinculo com servico, staff, cliente |

**Endpoints**
- `GET /services` — listar servicos da empresa.
- `POST /services` — criar servico.
- `PUT /services/:id` — atualizar servico.
- `GET /services/:id/staff` — listar profissionais habilitados.
- `POST /staff-skills` — vincular habilidade a profissional.
- `DELETE /staff-skills/:userId/:serviceId` — remover habilidade.
- `GET /appointments?staffId=&date=&status=` — listar agendamentos.
- `POST /appointments` — criar agendamento.
- `PATCH /appointments/:id/status` — transicionar status.
- `GET /staff/:userId/availability?serviceId=&date=` — horarios disponiveis.

**Queries especializadas**
- **Disponibilidade**: buscar slots livres considerando duracao do servico e agendamentos existentes do profissional no dia.
- **Agenda do dia**: agendamentos por profissional agrupados por horario.

**Regras de dominio**
- Agendamento so pode ser criado para profissional com StaffSkill correspondente.
- Nao permitir sobreposicao de horarios para o mesmo profissional.
- Appointment.orderId vincula o servico ao fluxo de pagamento padrao.

---

## 4D — Hospitality (Hotelaria)

### Tabelas envolvidas
`room_type` · `room_unit` · `rate_plan` · `rate_plan_price` · `reservation` · `stay_guest` · `folio` · `folio_charge`

### Funcionalidades de nivel medio
- Tipos de quarto com capacidade e tarifa base.
- Unidades fisicas de quarto (numero, andar, status).
- Planos tarifarios com politicas (cancelamento, deposito).
- Precos por tipo de quarto + plano + data (yield management).
- Reservas com check-in/check-out e status completo.
- Hospedes adicionais por reserva.
- Folio (folha de conta) com lancamentos de consumo.
- Vinculo de lancamentos do folio com vendas do POS.

### Funcionalidades de nivel baixo

**CRUD**
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | RoomType | Por companyId; codigo, nome, capacidade, tarifa base |
| CRUD | RoomUnit | Por companyId + roomTypeId; codigo unico, andar, ativo |
| CRUD | RatePlan | Por companyId; nome e politicas (jsonb) |
| CRUD | RatePlanPrice | Par (ratePlanId, roomTypeId, date) unico |
| CRUD | Reservation | Por companyId; vinculo com cliente, tipo, plano |
| CRUD | StayGuest | Hospede adicional por reserva |
| Auto | Folio | Criado ao confirmar reserva |
| CRUD | FolioCharge | Lancamento no folio; saleId opcional |

**Endpoints**
- `GET /room-types` — listar tipos de quarto.
- `POST /room-types` — criar tipo.
- `GET /room-units?type=&floor=&available=` — listar unidades com filtros.
- `POST /room-units` — criar unidade.
- `GET /rate-plans` — listar planos tarifarios.
- `POST /rate-plans` — criar plano.
- `POST /rate-plans/:id/prices` — definir preco por tipo+data.
- `GET /availability?checkIn=&checkOut=&roomTypeId=` — consultar disponibilidade.
- `POST /reservations` — criar reserva.
- `PATCH /reservations/:id/status` — transicionar: pending → guaranteed → in_house → checked_out.
- `POST /reservations/:id/guests` — adicionar hospede.
- `GET /reservations/:id/folio` — ver folio com lancamentos.
- `POST /folios/:id/charges` — lancar consumo no folio.
- `POST /folios/:id/close` — fechar folio.

**Queries especializadas**
- **Disponibilidade**: contar RoomUnits ativas do tipo solicitado menos reservas ativas no periodo.
- **Tarifa do periodo**: somar RatePlanPrice por noite entre checkIn e checkOut.
- **Ocupacao**: percentual de unidades com reserva in_house sobre total ativo.

**Regras de dominio**
- Reserva so pode ser criada se houver unidade disponivel do tipo no periodo.
- Folio e criado automaticamente ao confirmar reserva; fechado no check-out.
- FolioCharge com saleId vincula consumo ao POS (frigobar, restaurante, etc.).
- StayGuest nao pode exceder capacidade do RoomType.
- Check-out fecha o folio e gera cobranca pendente se balance > 0.

---

## 4E — Events & Ticketing

### Tabelas envolvidas
`venue` · `event` · `ticket_type` · `ticket`

### Funcionalidades de nivel medio
- Gestao de locais (venues) com endereco.
- Criacao de eventos com data/hora de inicio e fim.
- Tipos de ingresso por evento com preco e capacidade.
- Emissao de ingressos vinculada a venda.
- Controle de status: active, used, cancelled, refunded.

### Funcionalidades de nivel baixo

**CRUD**
| Operacao | Entidade | Observacoes |
|----------|----------|-------------|
| CRUD | Venue | Por companyId; nome e endereco |
| CRUD | Event | Por companyId + venueId; nome, datas, descricao |
| CRUD | TicketType | Por eventId; nome, preco, capacidade |
| Auto | Ticket | Criado via venda; vincula cliente e tipo |

**Endpoints**
- `GET /venues` — listar locais.
- `POST /venues` — criar local.
- `GET /events` — listar eventos com filtros (data, venue).
- `POST /events` — criar evento.
- `PUT /events/:id` — atualizar evento.
- `GET /events/:id/ticket-types` — listar tipos de ingresso.
- `POST /events/:id/ticket-types` — criar tipo de ingresso.
- `PUT /ticket-types/:id` — atualizar tipo (preco, capacidade).
- `GET /ticket-types/:id/availability` — ingressos disponiveis.
- `POST /tickets` — emitir ingresso (vinculado a saleId).
- `PATCH /tickets/:id/status` — marcar como used/cancelled/refunded.
- `GET /tickets/:id` — detalhes do ingresso (validacao na entrada).
- `GET /customers/:id/tickets` — ingressos do cliente.

**Queries especializadas**
- **Disponibilidade**: capacity - count(tickets WHERE status = active OR status = used) por tipo.
- **Vendas por evento**: soma de tickets emitidos agrupados por tipo.

**Regras de dominio**
- Ingresso so pode ser emitido se disponibilidade > 0.
- Ticket.saleId e obrigatorio — todo ingresso passa pelo fluxo de venda.
- Status `used` e terminal para validacao na entrada.
- Cancelamento/reembolso de ticket deve refletir no status da venda correspondente.
