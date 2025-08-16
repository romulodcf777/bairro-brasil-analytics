// Configuração básica
const API = ""; // usa a mesma origem da página

// Funções auxiliares
const $ = (sel) => document.querySelector(sel);
const $$ = (sel) => Array.from(document.querySelectorAll(sel));

// Seções da página
const secoes = {
  register: $("#section-register"),
  list: $("#section-list"),
  charts: $("#section-charts"),
};

// Função pra trocar de aba
function mostrarSecao(nome) {
  Object.values(secoes).forEach((s) => s.classList.remove("active"));
  secoes[nome].classList.add("active");
}

// Eventos dos botões do menu
$("#tab-register").onclick = () => mostrarSecao("register");
$("#tab-list").onclick = () => {
  mostrarSecao("list");
  carregarRegistros();
};
$("#tab-charts").onclick = () => {
  mostrarSecao("charts");
  desenharGrafico(window.dadosAtuais || []);
};

// Carrega as categorias disponíveis
async function carregarCategorias() {
  try {
    const resposta = await fetch(`${API}/api/categories`);
    const categorias = await resposta.json();
    const select = $("#category");
    select.innerHTML = "";

    categorias.forEach((cat) => {
      const opcao = document.createElement("option");
      opcao.value = cat.name;
      opcao.textContent = cat.name;
      select.appendChild(opcao);
    });
  } catch (erro) {
    console.error("Erro ao carregar categorias:", erro);
  }
}

// Carrega as categorias quando a página abre
carregarCategorias();

// Adicionar nova categoria
$("#btn-add-category").onclick = async () => {
  const nome = prompt("Digite o nome da nova categoria:");
  if (!nome || nome.trim() === "") return;

  try {
    const resposta = await fetch(`${API}/api/categories`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: nome.trim() }),
    });

    if (resposta.ok) {
      await carregarCategorias();
      alert("Categoria criada com sucesso!");
    } else {
      const erro = await resposta.json().catch(() => ({}));
      alert(erro.erro || "Não foi possível criar a categoria");
    }
  } catch (erro) {
    alert("Erro ao criar categoria. Tente novamente.");
  }
};

// Salvar novo registro
$("#form-record").addEventListener("submit", async (e) => {
  e.preventDefault();

  // Pega os dados do formulário
  const dataHora = $("#timestamp").value
    ? new Date($("#timestamp").value).toISOString()
    : null;

  const dados = {
    timestamp: dataHora,
    source: $("#source").value,
    categoryName: $("#category").value,
    amount: parseFloat($("#amount").value || "0"),
    notes: $("#notes").value || null,
  };

  try {
    const resposta = await fetch(`${API}/api/records`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dados),
    });

    if (resposta.ok) {
      alert("Registro salvo com sucesso!");
      e.target.reset(); // limpa o formulário
      carregarRegistros(); // atualiza a lista
    } else {
      const erro = await resposta.json().catch(() => ({}));
      alert(erro.erro || "Erro ao salvar o registro");
    }
  } catch (erro) {
    alert("Erro ao salvar. Verifique sua conexão.");
  }
});

// Carregar e exibir registros
async function carregarRegistros() {
  // Monta os parâmetros de filtro
  const params = new URLSearchParams();
  const dataInicial = $("#f-from").value;
  const dataFinal = $("#f-to").value;
  const estabelecimento = $("#f-source").value;
  const categoria = $("#f-category").value;

  if (dataInicial) params.set("from", dataInicial);
  if (dataFinal) params.set("to", dataFinal);
  if (estabelecimento) params.set("source", estabelecimento);
  if (categoria) params.set("category", categoria);

  try {
    const resposta = await fetch(`${API}/api/records?${params.toString()}`);
    const registros = await resposta.json();
    window.dadosAtuais = registros; // salva pra usar no gráfico

    const tabela = $("#table-records tbody");
    tabela.innerHTML = "";

    registros.forEach((registro) => {
      const linha = document.createElement("tr");
      linha.innerHTML = `
        <td>${new Date(registro.timestamp).toLocaleString("pt-BR")}</td>
        <td>${registro.source}</td>
        <td>${registro.category || "Sem categoria"}</td>
        <td>R$ ${registro.amount.toFixed(2).replace(".", ",")}</td>
        <td>${registro.notes || "-"}</td>
        <td><button class="excluir" data-id="${
          registro.id
        }">Excluir</button></td>
      `;
      tabela.appendChild(linha);
    });

    // Adiciona eventos de exclusão
    $$("button.excluir[data-id]").forEach((btn) => {
      btn.onclick = async () => {
        if (!confirm("Tem certeza que quer excluir este registro?")) return;

        const id = btn.getAttribute("data-id");
        try {
          const resposta = await fetch(`${API}/api/records/${id}`, {
            method: "DELETE",
          });
          if (resposta.ok) {
            carregarRegistros(); // recarrega a lista
          } else {
            alert("Erro ao excluir registro");
          }
        } catch (erro) {
          alert("Erro ao excluir. Tente novamente.");
        }
      };
    });

    // Atualiza o link de exportação
    const linkExport = $("#btn-export");
    linkExport.setAttribute(
      "href",
      `${API}/api/records/export.csv?${params.toString()}`
    );
  } catch (erro) {
    alert("Erro ao carregar registros");
  }
}

// Evento do botão de filtrar
$("#btn-filter").onclick = () => carregarRegistros();

// Desenhar gráfico simples (sem bibliotecas externas)
function desenharGrafico(registros) {
  const canvas = document.getElementById("chart");
  const ctx = canvas.getContext("2d");
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  // Agrupa os valores por categoria
  const totaisPorCategoria = new Map();
  registros.forEach((reg) => {
    const categoria = reg.category || "Sem categoria";
    const valorAtual = totaisPorCategoria.get(categoria) || 0;
    totaisPorCategoria.set(categoria, valorAtual + reg.amount);
  });

  const categorias = Array.from(totaisPorCategoria.keys());
  const valores = Array.from(totaisPorCategoria.values());

  if (categorias.length === 0) {
    ctx.fillStyle = "#34495e";
    ctx.font = "16px Arial";
    ctx.fillText(
      "Nenhum registro encontrado. Cadastre algumas vendas primeiro!",
      20,
      50
    );
    return;
  }

  // Configurações do gráfico
  const margemX = 60;
  const margemY = 40;
  const larguraGrafico = canvas.width - margemX * 2;
  const alturaGrafico = canvas.height - margemY * 2;
  const valorMaximo = Math.max(...valores) || 1;
  const larguraBarra = (larguraGrafico / categorias.length) * 0.7;
  const espacamento = (larguraGrafico / categorias.length) * 0.3;

  // Desenha as linhas de referência
  ctx.strokeStyle = "#bdc3c7";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(margemX, margemY);
  ctx.lineTo(margemX, margemY + alturaGrafico);
  ctx.lineTo(margemX + larguraGrafico, margemY + alturaGrafico);
  ctx.stroke();

  // Desenha as barras
  ctx.fillStyle = "#3498db";
  categorias.forEach((categoria, i) => {
    const x = margemX + i * (larguraBarra + espacamento) + espacamento / 2;
    const alturaRelativa = (valores[i] / valorMaximo) * (alturaGrafico - 20);
    const y = margemY + alturaGrafico - alturaRelativa;

    // Barra
    ctx.fillRect(x, y, larguraBarra, alturaRelativa);

    // Nome da categoria
    ctx.fillStyle = "#2c3e50";
    ctx.font = "12px Arial";
    ctx.fillText(categoria, x, margemY + alturaGrafico + 15);

    // Valor em cima da barra
    ctx.fillText(`R$ ${valores[i].toFixed(0)}`, x, y - 5);

    ctx.fillStyle = "#3498db";
  });
}

// Carrega os dados iniciais
carregarRegistros();
