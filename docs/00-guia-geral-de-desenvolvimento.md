# Guia Geral de Desenvolvimento

## Objetivo
Este documento define a base de decisao tecnica do projeto para manter uma unica fonte de verdade entre desenvolvedores e modelos de IA.

## Ordem de prioridades
As decisoes tecnicas devem seguir esta ordem:

1. KISS (manter simples)
2. SOLID (design orientado a manutencao)
3. DDD (modelagem orientada ao dominio)
4. TDD (desenvolvimento guiado por testes)
5. DRY, Clean Code e Clean Architecture
6. YAGNI (nao antecipar complexidade sem necessidade real)

> Regra pratica: quando houver conflito entre abordagens, preferir a mais simples que preserve SOLID e atenda os testes.

## Principios obrigatorios
- Nenhuma funcionalidade de producao deve existir sem testes cobrindo comportamento esperado.
- Implementacoes devem comecar no menor incremento possivel.
- Refatoracao acontece apos os testes estarem verdes, nunca antes.
- Evitar acoplamento desnecessario entre camadas e contextos.
- Nomear classes, servicos, metodos e modelos com foco no dominio.

## Criterios de qualidade
- Legibilidade acima de criatividade.
- Mudancas pequenas, com objetivo unico e verificavel.
- Dependencias externas so entram com justificativa tecnica clara.
- Todo codigo novo deve ser facil de remover, substituir ou extender.

## Definicao de pronto
Uma tarefa so e considerada pronta quando:
- possui testes automatizados representando cenarios relevantes;
- a implementacao minima passa nos testes;
- houve revisao de design e organizacao (refatoracao segura);
- os testes da solucao executam com sucesso.
