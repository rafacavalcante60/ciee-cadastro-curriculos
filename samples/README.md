# Amostras de currículo

Currículos fictícios para testar a extração de PDF, à mão pelo formulário ou
pelo Swagger. O teste de integração `ExtracaoDeCurriculoTestes` envia cada PDF
listado em `esperado.json` para `POST /api/curriculos/extracao` e compara a
resposta com o resultado declarado ali.

| Arquivo | O que exercita |
|---|---|
| `curriculo-completo.pdf` | Caminho feliz: nome, e-mail e telefone presentes. É o currículo fictício pedido pelo enunciado. |
| `curriculo-sem-telefone.pdf` | Telefone ausente volta `null`. Os períodos (`2021 – 2023`) não viram telefone. |
| `curriculo-nome-com-rotulo.pdf` | `Nome completo:` em maiúsculas, depois de um título "Dados Pessoais" que também parece nome. CPF de 11 dígitos sem pontuação, CEP com rótulo e celular com `+55`. |
| `curriculo-duas-colunas.pdf` | Coluna lateral com contato e idiomas, lida antes da coluna principal, onde está o nome. |
| `curriculo-digitalizado.pdf` | A página do currículo completo transformada em imagem, como sai de um scanner. Sem camada de texto: volta tudo `null` com aviso. |
| `curriculo-protegido-por-senha.pdf` | O currículo completo criptografado com senha de abertura: volta tudo `null` com aviso de senha. |
| `curriculo-corrompido.pdf` | O currículo completo cortado no primeiro terço. Passa pela assinatura `%PDF-`, mas a leitura falha: volta tudo `null` com aviso. |
| `nao-e-pdf.pdf` | Texto puro com extensão `.pdf`: a API recusa com 400 pela assinatura de bytes. Fica fora do `esperado.json`. |

O arquivo acima de 5 MB não está aqui: o teste o gera na hora, para não
versionar um binário grande.

## Gerar de novo

Os PDFs são gerados com [QuestPDF](https://www.questpdf.com/) pelo projeto em
`gerador/`, que fica fora da solução do backend para que `dotnet test` não
dependa dele:

```bash
cd samples/gerador && dotnet run
```

A geração é determinística, exceto `curriculo-protegido-por-senha.pdf`: a
criptografia sorteia um sal novo a cada vez, então só ele muda de bytes.

Ao mudar uma amostra, atualize `esperado.json` com o que o currículo contém, não
com o que a extração devolveu.
