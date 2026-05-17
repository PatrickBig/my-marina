// Sketchy wireframe primitives for MyMarina wireframes.
// Low-fi feel: handwritten fonts, wobbly/dashed borders, b&w with one accent.
// Components export to window for use in other Babel scripts.

const WF_INK = '#1c1a17';
const WF_INK_SOFT = '#5b554d';
const WF_INK_FAINT = '#a59f93';
const WF_PAPER = '#fbf8f1';
const WF_PAPER_LINE = '#e6e0d3';
const WF_ACCENT = 'oklch(60% 0.13 230)';   // navy-teal
const WF_ACCENT_SOFT = 'oklch(92% 0.04 230)';
const WF_HIGHLIGHT = 'oklch(94% 0.13 95)'; // soft yellow highlighter
const WF_BAD = 'oklch(60% 0.16 28)';
const WF_GOOD = 'oklch(60% 0.12 150)';

const wfFont = `"Kalam", "Architects Daughter", "Comic Sans MS", cursive`;
const wfFontHand = `"Caveat", "Kalam", cursive`;
const wfFontMono = `"JetBrains Mono", "Courier New", monospace`;

// ── Shell ────────────────────────────────────────────────────
// Page is the artboard root; gives paper bg + base text style.
function WFPage({ children, style = {}, dense = false }) {
  return (
    <div style={{
      width: '100%', height: '100%',
      background: WF_PAPER,
      color: WF_INK,
      fontFamily: wfFont,
      fontSize: dense ? 11 : 13,
      lineHeight: 1.35,
      position: 'relative',
      overflow: 'hidden',
      ...style,
    }}>{children}</div>
  );
}

// ── Sketchy borders ──────────────────────────────────────────
// Filter id in body for hand-drawn squiggle. Inject once.
if (typeof document !== 'undefined' && !document.getElementById('wf-defs')) {
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.id = 'wf-defs';
  svg.setAttribute('width', '0');
  svg.setAttribute('height', '0');
  svg.style.position = 'absolute';
  svg.innerHTML = `
    <defs>
      <filter id="wf-rough">
        <feTurbulence type="fractalNoise" baseFrequency="0.04" numOctaves="2" seed="3"/>
        <feDisplacementMap in="SourceGraphic" scale="1.6"/>
      </filter>
      <filter id="wf-rough-strong">
        <feTurbulence type="fractalNoise" baseFrequency="0.06" numOctaves="2" seed="5"/>
        <feDisplacementMap in="SourceGraphic" scale="3"/>
      </filter>
    </defs>`;
  document.body.appendChild(svg);
}

// Wobbly hand-drawn rectangle border using filter on SVG.
function WFBox({ children, style = {}, dashed = false, fill = 'transparent', strokeWidth = 1.5, rough = true, radius = 2, accent = false, onClick }) {
  return (
    <div onClick={onClick} style={{ position: 'relative', ...style }}>
      <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none', filter: rough ? 'url(#wf-rough)' : undefined }}>
        <rect x={strokeWidth} y={strokeWidth}
          width={`calc(100% - ${strokeWidth * 2}px)`}
          height={`calc(100% - ${strokeWidth * 2}px)`}
          rx={radius} ry={radius}
          fill={fill}
          stroke={accent ? WF_ACCENT : WF_INK}
          strokeWidth={strokeWidth}
          strokeDasharray={dashed ? '5 4' : undefined}
        />
      </svg>
      <div style={{ position: 'relative', height: '100%' }}>{children}</div>
    </div>
  );
}

// Inline ink line — used as separators
function WFLine({ vertical = false, dashed = false, color = WF_INK, style = {} }) {
  return (
    <div style={{
      background: color,
      width: vertical ? 1 : '100%',
      height: vertical ? '100%' : 1,
      opacity: 0.7,
      backgroundImage: dashed
        ? `repeating-linear-gradient(${vertical ? 'to bottom' : 'to right'}, ${color} 0 4px, transparent 4px 8px)`
        : undefined,
      backgroundColor: dashed ? 'transparent' : color,
      ...style,
    }} />
  );
}

// ── Buttons / chips ──────────────────────────────────────────
function WFButton({ children, primary = false, small = false, style = {}, ...rest }) {
  return (
    <button {...rest} style={{
      fontFamily: wfFont,
      fontSize: small ? 11 : 13,
      padding: small ? '4px 10px' : '7px 14px',
      background: primary ? WF_ACCENT : 'transparent',
      color: primary ? '#fff' : WF_INK,
      border: `1.5px solid ${primary ? WF_ACCENT : WF_INK}`,
      borderRadius: 3,
      cursor: 'pointer',
      filter: 'url(#wf-rough)',
      ...style,
    }}>{children}</button>
  );
}

function WFPill({ children, color = WF_INK, bg = 'transparent', style = {} }) {
  return (
    <span style={{
      display: 'inline-block',
      fontSize: 10,
      fontFamily: wfFontMono,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
      padding: '2px 7px',
      border: `1px solid ${color}`,
      color,
      background: bg,
      borderRadius: 99,
      filter: 'url(#wf-rough)',
      ...style,
    }}>{children}</span>
  );
}

function WFTag({ children, style = {} }) {
  return (
    <span style={{
      display: 'inline-block',
      fontSize: 10,
      fontFamily: wfFontMono,
      padding: '1px 5px',
      background: WF_HIGHLIGHT,
      color: WF_INK,
      ...style,
    }}>{children}</span>
  );
}

// ── Text helpers ─────────────────────────────────────────────
function WFTitle({ children, level = 1, style = {} }) {
  const sizes = { 1: 22, 2: 17, 3: 14 };
  return (
    <div style={{
      fontFamily: wfFontHand,
      fontSize: sizes[level],
      fontWeight: level === 1 ? 700 : 600,
      lineHeight: 1.05,
      letterSpacing: -0.2,
      color: WF_INK,
      ...style,
    }}>{children}</div>
  );
}

function WFLabel({ children, style = {} }) {
  return (
    <div style={{
      fontFamily: wfFontMono,
      fontSize: 9,
      textTransform: 'uppercase',
      letterSpacing: 0.8,
      color: WF_INK_SOFT,
      ...style,
    }}>{children}</div>
  );
}

function WFNote({ children, style = {} }) {
  return (
    <div style={{
      fontFamily: wfFontHand,
      fontSize: 14,
      color: WF_INK_SOFT,
      lineHeight: 1.2,
      ...style,
    }}>{children}</div>
  );
}

// Squiggly underline (like a marker highlight)
function WFUnderline({ children, color = WF_HIGHLIGHT, style = {} }) {
  return (
    <span style={{
      backgroundImage: `linear-gradient(transparent 60%, ${color} 60%, ${color} 95%, transparent 95%)`,
      ...style,
    }}>{children}</span>
  );
}

// ── Placeholder image (striped, with mono explainer) ────────
function WFPlaceholder({ label = 'image', style = {}, height = 80, vertical = false }) {
  const bg = `repeating-linear-gradient(${vertical ? '0deg' : '45deg'}, ${WF_PAPER_LINE} 0 6px, transparent 6px 12px)`;
  return (
    <div style={{
      height,
      background: bg,
      border: `1px dashed ${WF_INK_FAINT}`,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: wfFontMono,
      fontSize: 9,
      color: WF_INK_SOFT,
      textTransform: 'uppercase',
      letterSpacing: 0.8,
      ...style,
    }}>[{label}]</div>
  );
}

// Map placeholder — with sketchy coastline
function WFMap({ style = {}, pins = 6, label = 'map' }) {
  return (
    <div style={{
      position: 'relative',
      background: WF_PAPER,
      border: `1.5px solid ${WF_INK}`,
      filter: 'url(#wf-rough)',
      overflow: 'hidden',
      ...style,
    }}>
      {/* sketch coastline */}
      <svg style={{ position: 'absolute', inset: 0, width: '100%', height: '100%' }} preserveAspectRatio="none">
        <path d="M0,40 Q40,30 70,55 T140,60 T210,40 T280,70 T360,50 L360,200 L0,200 Z"
          fill={WF_PAPER_LINE} stroke={WF_INK_SOFT} strokeWidth="1" strokeDasharray="3 2"
          vectorEffect="non-scaling-stroke" filter="url(#wf-rough)" />
        <path d="M0,90 Q60,85 100,100 T200,95 T280,110 T360,95"
          stroke={WF_INK_FAINT} strokeWidth="0.8" fill="none" strokeDasharray="2 3"
          vectorEffect="non-scaling-stroke" />
      </svg>
      {/* pins */}
      {Array.from({ length: pins }).map((_, i) => {
        const x = 10 + (i * 17 + (i * i) * 3) % 80;
        const y = 15 + (i * 13 + i * 7) % 65;
        const big = i === 1;
        return (
          <div key={i} style={{
            position: 'absolute', left: `${x}%`, top: `${y}%`,
            width: big ? 22 : 14, height: big ? 22 : 14,
            borderRadius: '50% 50% 50% 0',
            transform: 'rotate(-45deg)',
            background: big ? WF_ACCENT : WF_INK,
            color: '#fff',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: big ? 10 : 8,
            fontFamily: wfFontMono,
            fontWeight: 700,
          }}>
            <span style={{ transform: 'rotate(45deg)' }}>{big ? '$' : '·'}</span>
          </div>
        );
      })}
      <div style={{ position: 'absolute', bottom: 6, right: 8, fontFamily: wfFontMono, fontSize: 9, color: WF_INK_SOFT, textTransform: 'uppercase', letterSpacing: 0.8 }}>
        [{label}]
      </div>
    </div>
  );
}

// ── Top app shell (used inside artboards) ────────────────────
function WFAppBar({ active = 'Find a slip', user = 'Maria R.', avatar = 'MR', persona = null }) {
  const items = ['Find a slip', 'My trips', 'My boats', 'My slips', 'Inbox'];
  return (
    <div style={{ borderBottom: `1.5px solid ${WF_INK}`, padding: '8px 18px', display: 'flex', alignItems: 'center', gap: 14, background: WF_PAPER }}>
      <div style={{ fontFamily: wfFontHand, fontWeight: 700, fontSize: 18, letterSpacing: -0.3 }}>
        ⚓ MyMarina
      </div>
      <div style={{ display: 'flex', gap: 12, marginLeft: 18 }}>
        {items.map(it => (
          <div key={it} style={{
            fontSize: 12,
            color: it === active ? WF_INK : WF_INK_SOFT,
            borderBottom: it === active ? `2px solid ${WF_ACCENT}` : '2px solid transparent',
            paddingBottom: 2,
          }}>{it}</div>
        ))}
      </div>
      <div style={{ flex: 1 }} />
      {persona && <WFPill color={WF_INK_SOFT}>{persona}</WFPill>}
      <div style={{ width: 22, height: 22, borderRadius: 99, border: `1.5px solid ${WF_INK}`, fontFamily: wfFontMono, fontSize: 9, display: 'flex', alignItems: 'center', justifyContent: 'center', filter: 'url(#wf-rough)' }}>{avatar}</div>
    </div>
  );
}

// Side rail (operator console)
function WFSideRail({ active, items, title = 'Big Bay Marina' }) {
  return (
    <div style={{ width: 150, borderRight: `1.5px solid ${WF_INK}`, padding: '14px 12px', display: 'flex', flexDirection: 'column', gap: 4, background: WF_PAPER }}>
      <div style={{ fontFamily: wfFontHand, fontWeight: 700, fontSize: 16, marginBottom: 2 }}>⚓ MyMarina</div>
      <WFLabel style={{ marginBottom: 10 }}>operator console</WFLabel>
      <div style={{ fontSize: 11, fontWeight: 600, marginBottom: 6, paddingBottom: 6, borderBottom: `1px dashed ${WF_INK_FAINT}` }}>{title}</div>
      {items.map(it => (
        <div key={it} style={{
          fontSize: 12,
          padding: '4px 8px',
          background: it === active ? WF_ACCENT_SOFT : 'transparent',
          borderLeft: it === active ? `2.5px solid ${WF_ACCENT}` : '2.5px solid transparent',
          color: it === active ? WF_INK : WF_INK_SOFT,
        }}>{it}</div>
      ))}
    </div>
  );
}

// Annotation arrow + caption (red-pen style)
function WFAnnotation({ children, style = {}, color = WF_BAD, rotate = -3 }) {
  return (
    <div style={{
      position: 'absolute',
      fontFamily: wfFontHand,
      fontSize: 13,
      color,
      transform: `rotate(${rotate}deg)`,
      ...style,
    }}>{children}</div>
  );
}

// Persona ribbon along the top of an artboard (so users know who's looking)
function WFPersonaRibbon({ persona, scenario }) {
  return (
    <div style={{
      position: 'absolute', top: 8, right: 8, zIndex: 5,
      background: WF_INK, color: WF_PAPER, padding: '3px 9px',
      fontFamily: wfFontMono, fontSize: 9,
      letterSpacing: 0.8, textTransform: 'uppercase',
      transform: 'rotate(2deg)',
    }}>
      {persona}{scenario ? ` · ${scenario}` : ''}
    </div>
  );
}

// ── Form bits ────────────────────────────────────────────────
function WFInput({ label, value, hint, style = {}, big = false, accent = false }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 3, ...style }}>
      {label && <WFLabel>{label}</WFLabel>}
      <WFBox accent={accent} style={{ height: big ? 36 : 28, display: 'flex', alignItems: 'center', padding: '0 10px', background: '#fff' }} rough={true}>
        <span style={{ fontFamily: wfFont, fontSize: big ? 14 : 12, color: value ? WF_INK : WF_INK_FAINT }}>
          {value || hint || ''}
        </span>
      </WFBox>
    </div>
  );
}

function WFCheckbox({ label, checked = false, style = {} }) {
  return (
    <label style={{ display: 'flex', alignItems: 'center', gap: 7, fontSize: 12, ...style }}>
      <span style={{
        display: 'inline-block', width: 13, height: 13,
        border: `1.5px solid ${WF_INK}`,
        background: checked ? WF_INK : 'transparent',
        color: '#fff', fontFamily: wfFontMono, fontSize: 10,
        textAlign: 'center', lineHeight: '11px',
        filter: 'url(#wf-rough)',
      }}>{checked ? '✓' : ''}</span>
      <span>{label}</span>
    </label>
  );
}

function WFToggle({ on = false, style = {} }) {
  return (
    <span style={{
      display: 'inline-block', width: 28, height: 14,
      borderRadius: 99,
      border: `1.5px solid ${WF_INK}`,
      background: on ? WF_ACCENT : 'transparent',
      position: 'relative',
      filter: 'url(#wf-rough)',
      ...style,
    }}>
      <span style={{
        position: 'absolute', top: 1, left: on ? 14 : 1,
        width: 10, height: 10, borderRadius: 99,
        background: on ? '#fff' : WF_INK,
      }} />
    </span>
  );
}

// Card (boxed item used in lists)
function WFCard({ children, style = {}, accent = false, dashed = false, padding = 12 }) {
  return (
    <WFBox accent={accent} dashed={dashed} style={{ background: '#fff', ...style }}>
      <div style={{ padding, height: '100%' }}>{children}</div>
    </WFBox>
  );
}

Object.assign(window, {
  WF_INK, WF_INK_SOFT, WF_INK_FAINT, WF_PAPER, WF_PAPER_LINE,
  WF_ACCENT, WF_ACCENT_SOFT, WF_HIGHLIGHT, WF_BAD, WF_GOOD,
  wfFont, wfFontHand, wfFontMono,
  WFPage, WFBox, WFLine, WFButton, WFPill, WFTag,
  WFTitle, WFLabel, WFNote, WFUnderline,
  WFPlaceholder, WFMap,
  WFAppBar, WFSideRail, WFAnnotation, WFPersonaRibbon,
  WFInput, WFCheckbox, WFToggle, WFCard,
});
