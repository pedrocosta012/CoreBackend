# Diretrizes Especificas do CoreBackend

## Escopo
Este documento define regras obrigatorias para o projeto de producao `CoreBackend`.

## Regras obrigatorias de implementacao
- Nao adicionar funcionalidade em producao sem teste correspondente.
- Seguir sempre: teste falhando -> implementacao minima -> refatoracao.
- Se o teste nao existe, a funcionalidade ainda nao esta autorizada para producao.
- Refatoracao deve reorganizar o codigo sem mudar comportamento validado.

## Dependencias de dados e performance
Dependencias principais de acesso a dados:
- `Dapper`
- `Dapper.SqlBuilder`

Justificativa:
- prioridade maxima em performance de leitura/escrita e controle fino de SQL;
- baixo overhead em relacao a ORMs mais abstratos;
- melhor previsibilidade para cenarios de alta carga.

## Convencoes de design
- Aplicar KISS como primeira regra de decisao.
- Preservar SOLID para evitar degradacao arquitetural ao longo do tempo.
- Usar DDD para proteger regras de negocio de detalhes de infraestrutura.
- Evitar sobreengenharia: criar apenas o que o caso atual exige.

## Regras para revisao tecnica
Toda entrega deve passar por uma revisao curta com foco em:
- simplicidade da solucao final;
- aderencia ao dominio;
- cobertura de testes da mudanca;
- ausencia de codigo sem uso ou abstrações prematuras;
- clareza de responsabilidade entre metodos, servicos, models e extensions.

## Anti-padroes proibidos
- Implementar "ja prevendo futuro" sem teste e sem demanda atual.
- Misturar regra de negocio com acesso a dados diretamente na mesma unidade.
- Introduzir complexidade para cobrir cenarios ainda nao existentes.
- Liberar codigo de producao com testes pendentes ou instaveis.
