# Specification Quality Checklist: Conta e dados do jogador

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-13
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Feature de infraestrutura com um lado visível (login e retomada). Termos de
  protocolo (`exp`, `iat`, `user_id`, 401, `kind`, `card_type`), nomes das portas
  da feature 001 e nomes do código antigo são vocabulário do domínio exigido pela
  constituição ("Código anterior" evolui no lugar), não escolha de implementação.
- Android Keystore e DPAPI aparecem em FR-021 porque a constituição os fixa;
  o caminho técnico (P/Invoke, provedor) fica para o research.
- SC-001 a SC-003 e SC-006 citam comando de teste, backend e `PlayerPrefs`
  porque são os critérios de pronto dados na descrição; continuam verificáveis
  sem conhecer a implementação.
- Contratos do backend citados por caminho, não copiados (pedido explícito).
- `/speckit-clarify` não deve rodar (pedido explícito). Decisões tomadas por
  padrão razoável e registradas em Assumptions, que merecem um olhar antes do
  plano:
  - margem de renovação de 30 s;
  - "resposta fora do contrato" como quinto desfecho do cliente autenticado;
  - segundo 401 após renovação bem-sucedida vira recusa, não expiração;
  - retomada recusada sem aviso de sessão expirada;
  - ler o perfil antes de trocar para a Home (o mini perfil hoje pode aparecer vazio);
  - guarda separada por jogador virtual do Multiplayer Play Mode (FR-024).
- Verificado no backend: SimpleJWT 5.5.1 sem configuração; claim `user_id` e
  `iat` presentes no token de acesso do login e da renovação.
