# Glossário

Vocabulário do domínio. O código, a interface e as mensagens usam estes termos,
em português, sem tradução na fronteira.

- **Candidato** — pessoa cadastrada no sistema para ser considerada em processos
  seletivos. É a única entidade persistida.
- **Currículo** — documento em PDF enviado pela pessoa do recrutamento. Serve
  apenas como fonte de texto para a extração e não é armazenado.
- **Extração** — leitura do texto de um currículo e tentativa de identificar
  nome, e-mail e telefone. O resultado é um palpite, sempre revisável.
- **Cadastro manual** — caminho em que a pessoa do recrutamento digita todos os
  dados, sem enviar arquivo.
- **Cadastro com PDF** — caminho em que o envio de um currículo preenche o
  formulário antes do salvamento. Não é um cadastro diferente: é o mesmo
  formulário, com os campos pré-preenchidos.
- **Campo não identificado** — campo que a extração não conseguiu encontrar no
  currículo. Fica vazio no formulário, para preenchimento manual, e nunca
  impede o cadastro.
