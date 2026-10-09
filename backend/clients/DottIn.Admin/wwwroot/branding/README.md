# Identidade DottIn

Os cinco SVGs originais foram fornecidos no kit `DottIn_SVG_Logo_Kit.zip`.
Os desenhos e o lettering são vetoriais; não dependem de fontes externas.

- `DottIn_Primary.svg`: marca navy e teal, fundo transparente.
- `DottIn_Symbol.svg`: símbolo compacto original.
- `DottIn_Dark.svg` / `DottIn_Light.svg`: variantes originais com fundo.
- `DottIn_Monochrome.svg`: impressão monocromática.
- `DottIn_White.svg`: variante derivada do Dark, sem retângulo de fundo,
  para manter contraste nas superfícies escuras do produto.
- `DottIn_Symbol_White.svg`: símbolo branco derivado para carregamento e app nativo.
- `DottIn_Favicon.svg`: símbolo adaptado ao tema claro/escuro do navegador.

O componente `DottInLogo` escolhe a versão pelo tema, reservando dimensões para
evitar deslocamento de layout. O mobile compartilha os mesmos arquivos via
`Content Link` no projeto, sem duplicar o kit. Ícone e splash nativos usam o
mesmo símbolo com fundo navy; o ícone adaptativo respeita a área segura.
