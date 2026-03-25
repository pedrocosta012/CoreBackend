# Fluxo de Trabalho DDD + TDD

## Objetivo
Padronizar o desenvolvimento orientado ao dominio com ciclo de testes primeiro, garantindo evolucao segura e previsivel.

## Ciclo obrigatorio por implementacao
1. Identificar regra de negocio no dominio.
2. Escrever teste que falha (red).
3. Implementar o minimo para o teste passar (green).
4. Refatorar estrutura sem alterar comportamento.
5. Reexecutar testes para validar regressao zero.

## DDD na pratica
- Organizar codigo por contexto de dominio, nao por tecnologia.
- Entidades e Value Objects devem refletir linguagem ubigua do negocio.
- Servicos de dominio devem encapsular regras que nao pertencem a uma entidade unica.
- Repositorios abstraem persistencia sem contaminar o dominio com detalhes de infraestrutura.

## TDD na pratica
- Todo bug corrigido deve ganhar teste de regressao antes da correcao final.
- Evitar testes acoplados a implementacao interna; testar comportamento observavel.
- Cobrir casos de sucesso, falha e borda.
- Garantir que testes sejam rapidos e deterministas.

## Regra de refatoracao
Apos o primeiro green:
- revisar nomes e fronteiras de responsabilidade;
- extrair metodos/servicos/extensions apenas quando houver ganho claro de coesao;
- remover duplicacao real (nao hipotetica);
- manter a solucao mais simples possivel.

## Checklist rapido antes de concluir uma tarefa
- O dominio ficou mais explicito?
- Existe teste para o comportamento adicionado/alterado?
- A implementacao atual e a menor que resolve o problema?
- A estrutura final ficou mais clara do que antes?
