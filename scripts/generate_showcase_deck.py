from pathlib import Path

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.util import Inches, Pt


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs" / "enterprise-rag-showcase.pptx"

W, H = 13.333, 7.5
NAVY = "101B2D"
NAVY2 = "17263B"
INK = "152338"
MUTED = "62728A"
PAPER = "F4F7FB"
WHITE = "FFFFFF"
TEAL = "21C4B7"
CYAN = "62D9E7"
LIME = "C5F36B"
ORANGE = "FFB86B"
BORDER = "DCE4EF"
SOFT_TEAL = "E4F8F5"
SOFT_BLUE = "E9F1FC"
SOFT_ORANGE = "FFF2E5"


def rgb(value):
    return RGBColor.from_string(value)


def new_slide(prs, dark=False):
    slide = prs.slides.add_slide(prs.slide_layouts[6])
    slide.background.fill.solid()
    slide.background.fill.fore_color.rgb = rgb(NAVY if dark else PAPER)
    return slide


def rect(slide, x, y, w, h, fill, radius=True, line=None):
    kind = MSO_SHAPE.ROUNDED_RECTANGLE if radius else MSO_SHAPE.RECTANGLE
    shape = slide.shapes.add_shape(kind, Inches(x), Inches(y), Inches(w), Inches(h))
    shape.fill.solid()
    shape.fill.fore_color.rgb = rgb(fill)
    if line:
        shape.line.color.rgb = rgb(line)
        shape.line.width = Pt(1)
    else:
        shape.line.fill.background()
    return shape


def circle(slide, x, y, d, fill, line=None):
    shape = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(x), Inches(y), Inches(d), Inches(d))
    shape.fill.solid()
    shape.fill.fore_color.rgb = rgb(fill)
    if line:
        shape.line.color.rgb = rgb(line)
        shape.line.width = Pt(1)
    else:
        shape.line.fill.background()
    return shape


def text(slide, x, y, w, h, value, size=16, color=INK, bold=False,
         font="Aptos", align=PP_ALIGN.LEFT, valign=MSO_ANCHOR.TOP,
         margin=0, italic=False):
    shape = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    frame = shape.text_frame
    frame.clear()
    frame.word_wrap = True
    frame.margin_left = Inches(margin)
    frame.margin_right = Inches(margin)
    frame.margin_top = Inches(margin)
    frame.margin_bottom = Inches(margin)
    frame.vertical_anchor = valign
    for index, line in enumerate(value.split("\n")):
        paragraph = frame.paragraphs[0] if index == 0 else frame.add_paragraph()
        paragraph.text = line
        paragraph.alignment = align
        paragraph.space_after = Pt(5)
        paragraph.font.name = font
        paragraph.font.size = Pt(size)
        paragraph.font.bold = bold
        paragraph.font.italic = italic
        paragraph.font.color.rgb = rgb(color)
    return shape


def pill(slide, x, y, w, label, fill, color=INK, size=10):
    rect(slide, x, y, w, 0.32, fill, True)
    text(slide, x + 0.08, y + 0.015, w - 0.16, 0.27, label, size, color, True,
         align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)


def header(slide, eyebrow, title, subtitle=""):
    text(slide, 0.72, 0.44, 11.9, 0.24, eyebrow.upper(), 10, TEAL, True)
    text(slide, 0.72, 0.82, 12.0, 0.55, title, 26, INK, True)
    if subtitle:
        text(slide, 0.74, 1.48, 11.8, 0.48, subtitle, 12, MUTED)


def footer(slide, number, dark=False):
    color = "9AAAC0" if dark else MUTED
    text(slide, 0.72, 7.13, 8.5, 0.18, "ENTERPRISE RAG PLATFORM  /  LOCAL-FIRST PROTOTYPE", 8, color, True)
    text(slide, 12.0, 7.09, 0.55, 0.22, f"{number:02d}", 9, color, True, align=PP_ALIGN.RIGHT)


def card(slide, x, y, w, h, title, body, accent=TEAL, fill=WHITE,
         title_size=15, body_size=11, body_color=MUTED):
    rect(slide, x, y, w, h, fill, True, BORDER if fill == WHITE else None)
    rect(slide, x + 0.17, y + 0.2, 0.07, h - 0.4, accent, False)
    title_color = WHITE if fill in (NAVY, NAVY2) else INK
    text(slide, x + 0.38, y + 0.2, w - 0.58, 0.36, title, title_size, title_color, True)
    text(slide, x + 0.38, y + 0.68, w - 0.58, h - 0.82, body, body_size, body_color)


def step(slide, x, y, w, index, title, detail, accent=TEAL, dark=False):
    base = NAVY2 if dark else WHITE
    rect(slide, x, y, w, 1.47, base, True, None if dark else BORDER)
    circle(slide, x + 0.18, y + 0.17, 0.34, accent)
    text(slide, x + 0.18, y + 0.17, 0.34, 0.34, str(index), 11, NAVY, True,
         align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
    text(slide, x + 0.18, y + 0.62, w - 0.36, 0.31, title, 13, WHITE if dark else INK, True)
    text(slide, x + 0.18, y + 0.98, w - 0.36, 0.38, detail, 9, "B8C6D9" if dark else MUTED)


def arrow(slide, x, y, label=">", color=TEAL):
    text(slide, x, y, 0.34, 0.34, label, 19, color, True, align=PP_ALIGN.CENTER)


def make_deck():
    prs = Presentation()
    prs.slide_width = Inches(W)
    prs.slide_height = Inches(H)
    prs.core_properties.title = "Enterprise RAG Platform | GitHub Showcase"
    prs.core_properties.subject = "Local-first RAG prototype architecture, implementation, demo, and roadmap"
    prs.core_properties.author = "Enterprise RAG Platform contributors"
    prs.core_properties.keywords = "RAG, .NET, React, PostgreSQL, Ollama, Azure AI Search"

    # 1. Cover
    s = new_slide(prs, True)
    circle(s, 9.55, 0.55, 3.2, NAVY2)
    circle(s, 10.1, 1.1, 2.1, "203850")
    pill(s, 0.82, 0.82, 2.15, "LOCAL-FIRST  /  EVIDENCE-GROUNDED", TEAL, NAVY, 9)
    text(s, 0.82, 1.55, 7.3, 1.5, "Enterprise RAG\nPlatform", 38, WHITE, True)
    text(s, 0.86, 3.4, 6.7, 0.8, "Grounded answers.\nTraceable evidence.", 22, "D5E1F0", False)
    text(s, 0.86, 5.55, 7.4, 0.35, ".NET 10  |  React + TypeScript  |  PostgreSQL  |  Ollama", 11, CYAN, True)
    text(s, 0.86, 6.15, 7.8, 0.25, "github.com/shashank2310/Enterprise-RAG-Platform", 10, "A9B8CB")
    # Floating evidence cards
    rect(s, 8.75, 1.28, 3.25, 0.78, WHITE)
    circle(s, 8.98, 1.53, 0.28, TEAL)
    text(s, 9.4, 1.42, 2.3, 0.34, "SOURCE DOCUMENT", 9, MUTED, True)
    text(s, 9.4, 1.74, 2.3, 0.22, "Policy / Runbook / PDF", 11, INK, True)
    arrow(s, 10.05, 2.22, "v", CYAN)
    rect(s, 8.75, 2.58, 3.25, 0.82, NAVY2, True, "36516D")
    text(s, 9.02, 2.74, 2.7, 0.27, "RETRIEVED EVIDENCE", 9, CYAN, True)
    text(s, 9.02, 3.08, 2.7, 0.2, "Top passages + chunk IDs", 11, WHITE)
    arrow(s, 10.05, 3.57, "v", LIME)
    rect(s, 8.75, 3.92, 3.25, 0.96, TEAL, True)
    text(s, 9.02, 4.1, 2.7, 0.28, "ANSWER + CITATIONS", 10, NAVY, True)
    text(s, 9.02, 4.45, 2.7, 0.22, "Grounded, inspectable output", 11, NAVY)
    text(s, 0.86, 6.92, 6.5, 0.2, "PORTFOLIO SHOWCASE  /  IMPLEMENTED LOCAL PATH + HONEST ROADMAP", 8, "9AAAC0", True)

    # 2. Product thesis
    s = new_slide(prs)
    header(s, "01 / The product thesis", "Enterprise answers need evidence, not just fluency",
           "A useful RAG system connects the original document to each answer and makes the retrieval path inspectable.")
    card(s, 0.78, 2.23, 3.8, 2.0, "The challenge", "Important knowledge is spread across long policies, procedures, and operational documents. A fluent response without source evidence is hard to trust.", ORANGE, SOFT_ORANGE, 16, 12, INK)
    card(s, 4.78, 2.23, 3.8, 2.0, "The experience", "Upload a document, ask a natural-language question, receive an answer constrained to retrieved passages, and inspect its source citations.", TEAL, SOFT_TEAL, 16, 12, INK)
    card(s, 8.78, 2.23, 3.8, 2.0, "The engineering", "Keep API, application, domain, and infrastructure concerns separated; swap local and cloud providers behind interfaces.", CYAN, SOFT_BLUE, 16, 12, INK)
    rect(s, 0.78, 4.76, 11.8, 1.05, NAVY, True)
    text(s, 1.08, 5.02, 11.15, 0.52, "DESIGN PRINCIPLE     Retrieve first. Generate from evidence. Validate every citation.", 17, WHITE, True, valign=MSO_ANCHOR.MIDDLE)
    footer(s, 2)

    # 3. What works now
    s = new_slide(prs)
    header(s, "02 / Working implementation", "An end-to-end local RAG loop", "A functioning developer demo spans ingestion, retrieval, generation, and a user-facing workflow.")
    card(s, 0.78, 2.18, 5.75, 1.55, "01  Ingest", "TXT, HTML, DOCX, and selectable-text PDF. Recursive chunks with 1,200-character target and 180-character overlap.", TEAL, WHITE, 15, 11)
    card(s, 6.78, 2.18, 5.75, 1.55, "02  Embed + persist", "Ollama embeddings stored with document and chunk metadata in PostgreSQL JSONB.", CYAN, WHITE, 15, 11)
    card(s, 0.78, 4.05, 5.75, 1.55, "03  Retrieve", "Tenant-context-scoped cosine similarity, ranked in the API; top-K is bounded (default 8, maximum 20).", LIME, WHITE, 15, 11)
    card(s, 6.78, 4.05, 5.75, 1.55, "04  Answer + cite", "Ollama chat receives retrieved passages. Citation markers are checked against the actual retrieved chunk IDs.", ORANGE, WHITE, 15, 11)
    pill(s, 0.84, 6.0, 2.08, "LOCAL PROTOTYPE", SOFT_TEAL, INK, 9)
    text(s, 3.12, 6.02, 9.4, 0.35, "Small-corpus demo; not production-scale vector search or a calibrated evaluation system.", 11, MUTED)
    footer(s, 3)

    # 4. Architecture
    s = new_slide(prs, True)
    text(s, 0.72, 0.42, 11.9, 0.24, "03 / SYSTEM ARCHITECTURE", 10, TEAL, True)
    text(s, 0.72, 0.8, 12, 0.55, "One request path. Two model calls. One evidence store.", 25, WHITE, True)
    text(s, 0.74, 1.44, 11.9, 0.4, "The API mediates model and database calls; Azure adapters are not a live cloud deployment.", 12, "B8C6D9")
    # user and UI
    rect(s, 0.78, 2.36, 2.15, 1.0, "203850", True, "36516D")
    text(s, 0.98, 2.56, 1.75, 0.28, "REACT SPA", 12, CYAN, True, align=PP_ALIGN.CENTER)
    text(s, 0.98, 2.91, 1.75, 0.25, "Upload + ask", 10, WHITE, align=PP_ALIGN.CENTER)
    arrow(s, 3.02, 2.69)
    rect(s, 3.43, 2.2, 2.55, 1.32, "203850", True, "36516D")
    text(s, 3.63, 2.42, 2.15, 0.28, ".NET API", 13, WHITE, True, align=PP_ALIGN.CENTER)
    text(s, 3.63, 2.79, 2.15, 0.42, "Auth policies\nIngestion + orchestration", 10, "B8C6D9", align=PP_ALIGN.CENTER)
    arrow(s, 6.08, 2.69)
    rect(s, 6.5, 2.2, 2.75, 1.32, "203850", True, "36516D")
    text(s, 6.7, 2.42, 2.35, 0.28, "OLLAMA", 13, TEAL, True, align=PP_ALIGN.CENTER)
    text(s, 6.7, 2.79, 2.35, 0.42, "Embedding model\nChat model", 10, "B8C6D9", align=PP_ALIGN.CENTER)
    arrow(s, 9.35, 2.69)
    rect(s, 9.78, 2.2, 2.75, 1.32, "203850", True, "36516D")
    text(s, 9.98, 2.42, 2.35, 0.28, "POSTGRESQL", 13, LIME, True, align=PP_ALIGN.CENTER)
    text(s, 9.98, 2.79, 2.35, 0.42, "Docs + chunks\nJSONB embeddings", 10, "B8C6D9", align=PP_ALIGN.CENTER)
    # Separate staged pipelines
    rect(s, 0.78, 4.12, 5.75, 1.35, NAVY2, True, "36516D")
    text(s, 1.02, 4.34, 5.15, 0.27, "INGESTION", 10, CYAN, True)
    text(s, 1.02, 4.76, 5.15, 0.42, "Upload  >  Extract  >  Chunk  >  Embed  >  Store", 14, WHITE, True)
    rect(s, 6.78, 4.12, 5.75, 1.35, NAVY2, True, "36516D")
    text(s, 7.02, 4.34, 5.15, 0.27, "QUESTION ANSWERING", 10, TEAL, True)
    text(s, 7.02, 4.76, 5.15, 0.42, "Question  >  Embed  >  Retrieve  >  Ground  >  Cite", 14, WHITE, True)
    text(s, 0.82, 5.94, 11.7, 0.5, "Optional OCR: Azure Document Intelligence for image-only PDFs. Azure OpenAI / AI Search adapters are not the active local path.", 11, "B8C6D9")
    footer(s, 4, True)

    # 5. Ingestion
    s = new_slide(prs)
    header(s, "04 / Ingestion pipeline", "Turn mixed-format files into retrievable evidence", "The local path is synchronous and deliberately simple enough to inspect end to end.")
    xs = [0.78, 3.27, 5.76, 8.25, 10.74]
    titles = ["Accept", "Extract", "Chunk", "Embed", "Persist"]
    details = ["20 MB service limit\nAllowed extensions", "TXT / HTML / DOCX\nPDF text locally", "Recursive\n1,200 target / 180 overlap", "Ollama\nBatches of 32", "PostgreSQL\ntransactional writes"]
    accents = [ORANGE, TEAL, CYAN, LIME, TEAL]
    for i, x in enumerate(xs):
        step(s, x, 2.55, 2.0, i + 1, titles[i], details[i], accents[i])
        if i < 4:
            arrow(s, x + 2.05, 3.1)
    rect(s, 0.8, 4.62, 11.72, 1.2, SOFT_ORANGE, True)
    text(s, 1.1, 4.82, 2.3, 0.26, "PDF BOUNDARY", 10, "A76522", True)
    text(s, 3.0, 4.8, 9.15, 0.62, "Selectable text is extracted locally with PdfPig. Scanned/image-only pages need configured Azure OCR; missing OCR does not block ordinary text PDFs.", 12, INK)
    text(s, 0.84, 6.15, 11.4, 0.28, "HTML scripts, styles, and noscript nodes are removed before visible text is extracted.", 11, MUTED)
    footer(s, 5)

    # 6. Retrieval/generation
    s = new_slide(prs, True)
    text(s, 0.72, 0.42, 11.9, 0.24, "05 / RETRIEVAL + GENERATION", 10, TEAL, True)
    text(s, 0.72, 0.8, 12, 0.55, "Make the evidence path visible", 26, WHITE, True)
    text(s, 0.74, 1.44, 11.8, 0.38, "A generated response is only useful when its supporting passages can be inspected.", 12, "B8C6D9")
    steps = [
        ("Question", "Natural language\nfrom the user", TEAL),
        ("Query vector", "Embedding model\nsame vector space", CYAN),
        ("Top-K", "Cosine-ranked\ntenant chunks", LIME),
        ("Ground", "Evidence + stable\nchunk IDs", ORANGE),
        ("Cite", "Only IDs present\nin retrieved context", TEAL),
    ]
    for i, (title, detail, color) in enumerate(steps):
        x = 0.78 + i * 2.5
        step(s, x, 2.35, 2.0, i + 1, title, detail, color, True)
        if i < 4:
            arrow(s, x + 2.05, 2.88)
    rect(s, 0.8, 4.42, 7.25, 1.55, NAVY2, True, "36516D")
    text(s, 1.08, 4.67, 6.65, 0.3, "GUARDRAIL", 10, LIME, True)
    text(s, 1.08, 5.08, 6.55, 0.63, "Validate model source labels; if omitted, attach the highest-ranked retrieved chunk rather than discard the answer.", 13, WHITE, True)
    rect(s, 8.35, 4.42, 4.17, 1.55, "3A2D2A", True)
    text(s, 8.62, 4.67, 3.6, 0.3, "HONEST LIMIT", 10, ORANGE, True)
    text(s, 8.62, 5.08, 3.6, 0.63, "Confidence is a placeholder (0), not a calibrated probability.", 13, WHITE, True)
    footer(s, 6, True)

    # 7. Security / trust boundary
    s = new_slide(prs)
    header(s, "06 / Security & trust boundaries", "Safe defaults for a local developer demo", "The demo is intentionally useful without cloud credentials, but it is not a production identity or tenancy solution.")
    card(s, 0.78, 2.2, 3.8, 2.05, "Local demo identity", "Enabled only in Development. Grants fixed demo query/ingest roles. UI/API ports bind to host loopback; PostgreSQL stays inside Compose.", TEAL, WHITE, 15, 11)
    card(s, 4.78, 2.2, 3.8, 2.05, "Configured Entra path", "MSAL SPA and API JWT authentication integration paths, with separate query and ingest role policies.", CYAN, WHITE, 15, 11)
    card(s, 8.78, 2.2, 3.8, 2.05, "Tenant context", "Identity-derived tenant context scopes local PostgreSQL read/write transactions. Production isolation still needs review and tests.", LIME, WHITE, 15, 11)
    rect(s, 0.8, 4.75, 11.75, 0.98, "FFF1E2", True)
    text(s, 1.12, 4.98, 11.0, 0.43, "Never expose local demo auth or the unauthenticated Ollama endpoint to an untrusted network.", 14, "8C4B18", True, valign=MSO_ANCHOR.MIDDLE)
    text(s, 0.84, 6.05, 11.3, 0.48, "Production work remains: security review, authorization/tenant-isolation tests, managed identity, private networking, secret management, and operational controls.", 11, MUTED)
    footer(s, 7)

    # 8. Run locally
    s = new_slide(prs)
    header(s, "07 / Developer experience", "From clone to first question", "Three local services plus a host-installed Ollama runtime. No OpenAI key is required for this path.")
    rect(s, 0.78, 2.15, 6.05, 3.95, NAVY, True)
    text(s, 1.1, 2.43, 5.35, 0.28, "POWERSHELL", 10, TEAL, True)
    code = "Copy-Item .env.example .env\n# set a unique POSTGRES_PASSWORD\n\nollama pull qwen3:1.7b\nollama pull nomic-embed-text\n\ndocker compose up --build"
    text(s, 1.1, 2.9, 5.4, 2.8, code, 15, WHITE, False, font="Consolas")
    card(s, 7.15, 2.15, 5.35, 1.1, "1  Start Ollama", "Keep Ollama running on the host machine.", TEAL, WHITE, 13, 10)
    card(s, 7.15, 3.49, 5.35, 1.1, "2  Launch the stack", "Compose starts API, PostgreSQL, and React frontend.", CYAN, WHITE, 13, 10)
    card(s, 7.15, 4.83, 5.35, 1.1, "3  Upload + stream", "Select multiple files; watch per-file indexing and live answer progress.", LIME, WHITE, 13, 10)
    text(s, 0.84, 6.35, 11.3, 0.3, "The API reaches host Ollama at host.docker.internal:11434; on Windows, follow the README firewall guidance if it cannot connect.", 10, MUTED)
    footer(s, 8)

    # 9. Validation corpus
    s = new_slide(prs, True)
    text(s, 0.72, 0.42, 11.9, 0.24, "08 / VALIDATION STORY", 10, TEAL, True)
    text(s, 0.72, 0.8, 12, 0.55, "Test more than the happy path", 26, WHITE, True)
    text(s, 0.74, 1.44, 11.8, 0.38, "A synthetic three-document corpus helps exercise extraction, chunking, retrieval, and evidence quality.", 12, "B8C6D9")
    card(s, 0.78, 2.2, 3.8, 1.65, "Travel expense policy", "Exact amounts, dates, approval thresholds, exception rules.", TEAL, NAVY2, 14, 11, "B8C6D9")
    card(s, 4.78, 2.2, 3.8, 1.65, "Platform operations", "Troubleshooting steps, retrieval boundaries, model configuration.", CYAN, NAVY2, 14, 11, "B8C6D9")
    card(s, 8.78, 2.2, 3.8, 1.65, "Support playbook (HTML)", "Severity targets, escalation, plus script/style extraction checks.", LIME, NAVY2, 14, 11, "B8C6D9")
    text(s, 0.82, 4.35, 5.35, 0.27, "VALIDATION PROMPTS", 10, TEAL, True)
    text(s, 0.82, 4.78, 5.45, 1.05, "Exact fact: mileage rate + date\nComposed rule: who approves an international trip?\nDistinction: response target vs. resolution guarantee", 12, WHITE)
    rect(s, 6.75, 4.35, 5.75, 1.55, "3A2D2A", True)
    text(s, 7.02, 4.58, 5.15, 0.25, "NEGATIVE TEST", 10, ORANGE, True)
    text(s, 7.02, 4.98, 5.0, 0.68, "Ask for a release date the documents never give. Check for abstention—not invention.", 13, WHITE, True)
    text(s, 0.84, 6.25, 11.4, 0.28, "Manual validation examples; not a scored benchmark or automated quality gate.", 10, "B8C6D9")
    footer(s, 9, True)

    # 10. Engineering tradeoffs
    s = new_slide(prs)
    header(s, "09 / Integration paths", "Adapters are building blocks—not a deployed service", "The next step is to complete the secure write path, identity, and operations around the existing integrations.")
    cols = [(0.8, "Models", "Azure OpenAI", "Chat and embedding adapters use DefaultAzureCredential. Finish Azure-provider service wiring, deployment setup, and vector-dimension checks.", TEAL),
            (4.78, "Retrieval", "Azure AI Search", "A tenant/group-filtered query adapter exists. Add index schema, chunk upsert/delete, ACL metadata, reindexing, and end-to-end tests.", CYAN),
            (8.76, "Ingestion", "Blob + Service Bus", "SDK clients can be registered but are not used by upload. Build private source storage, a durable worker, retries, and dead-letter handling.", ORANGE)]
    for x, label, title, body, accent in cols:
        pill(s, x, 2.15, 1.05, label.upper(), SOFT_TEAL if accent == TEAL else SOFT_BLUE if accent == CYAN else SOFT_ORANGE, INK, 8)
        card(s, x, 2.62, 3.72, 2.12, title, body, accent, WHITE, 15, 10)
    rect(s, 0.8, 5.15, 11.7, 0.9, NAVY, True)
    text(s, 1.1, 5.38, 11.1, 0.42, "OCR, Entra/MSAL, and Azure Monitor also have integration paths. No cloud resources or complete Azure runtime are provisioned here.", 12, WHITE, True, valign=MSO_ANCHOR.MIDDLE)
    footer(s, 10)

    # 11. Roadmap
    s = new_slide(prs, True)
    text(s, 0.72, 0.42, 11.9, 0.24, "10 / ROADMAP", 10, TEAL, True)
    text(s, 0.72, 0.8, 12, 0.55, "The path from local demo to production platform", 25, WHITE, True)
    text(s, 0.74, 1.44, 11.8, 0.38, "The runtime is current; next comes secure, scalable retrieval and measurable operations.", 12, "B8C6D9")
    roadmap = [
        ("01", "Tenant security", "Complete identity mapping and cross-tenant authorization tests.", TEAL),
        ("02", "Search at scale", "Azure AI Search lifecycle, hybrid retrieval, ACL filters.", CYAN),
        ("03", "Reliable ingestion", "Background jobs, retries, malware checks, source storage.", LIME),
        ("04", "Trust + operations", "Calibrated confidence, evaluation suite, tracing and cost.", ORANGE),
    ]
    for i, (num, title, body, accent) in enumerate(roadmap):
        y = 2.25 + i * 0.92
        circle(s, 0.9, y, 0.42, accent)
        text(s, 0.9, y, 0.42, 0.42, num, 9, NAVY, True, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
        text(s, 1.58, y - 0.01, 2.6, 0.33, title, 15, WHITE, True)
        text(s, 4.1, y + 0.01, 7.9, 0.4, body, 12, "B8C6D9")
        if i < 3:
            rect(s, 1.09, y + 0.46, 0.04, 0.4, "36516D", False)
    footer(s, 11, True)

    # 12. Portfolio close
    s = new_slide(prs)
    header(s, "11 / Portfolio summary", "A complete learning loop, built to be questioned", "Upload a document. Ask a hard question. Follow the answer back to its evidence.")
    rect(s, 0.8, 2.2, 7.3, 3.45, NAVY, True)
    text(s, 1.16, 2.54, 6.45, 0.32, "PROJECT TAKEAWAY", 10, TEAL, True)
    text(s, 1.16, 3.02, 6.45, 1.24, "Full-stack local RAG prototype with document ingestion, vector retrieval, grounded generation, and verifiable citations.", 23, WHITE, True)
    text(s, 1.16, 4.72, 6.4, 0.48, ".NET 10  /  React  /  PostgreSQL  /  Ollama  /  Docker", 12, CYAN, True)
    card(s, 8.48, 2.2, 4.0, 1.35, "GitHub repository", "github.com/shashank2310/Enterprise-RAG-Platform", TEAL, WHITE, 14, 10)
    card(s, 8.48, 3.87, 4.0, 1.35, "Demo corpus", "samples/rag-validation/", CYAN, WHITE, 14, 11)
    text(s, 8.55, 5.62, 3.85, 0.4, "See the README for setup;\nsee project-showcase.md for details.", 10, MUTED)
    footer(s, 12)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    prs.save(OUTPUT)
    print(f"Created {OUTPUT} ({len(prs.slides)} slides)")


if __name__ == "__main__":
    make_deck()
