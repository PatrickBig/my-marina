// Commercial marina onboarding wizard (6 frames):
// O0 Home entry (banner + draft card)
// O1 Step 1: Profile
// O2 Step 2: GPS location (geocode + Leaflet pin)
// O3 Step 3: Dock & slip bulk builder
// O4 Step 4: Preview & adjust table
// O5 Step 5: Publish

// Stepper used at the top of every wizard step
function WizStepper({ current = 1 }) {
  const steps = ['Profile', 'Location', 'Docks & slips', 'Preview', 'Publish'];
  return (
    <div style={{ padding: '10px 24px', borderBottom: `1px dashed ${WF_INK_FAINT}`, display: 'flex', alignItems: 'center', gap: 8 }}>
      {steps.map((s, i) => {
        const n = i + 1;
        const done = n < current;
        const here = n === current;
        return (
          <React.Fragment key={s}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span style={{
                width: 20, height: 20, borderRadius: 99,
                border: `1.5px solid ${here || done ? WF_INK : WF_INK_FAINT}`,
                background: done ? WF_INK : here ? WF_ACCENT : 'transparent',
                color: done || here ? '#fff' : WF_INK_FAINT,
                fontFamily: wfFontMono, fontSize: 10,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                filter: 'url(#wf-rough)',
              }}>{done ? '✓' : n}</span>
              <span style={{ fontSize: 12, color: here ? WF_INK : WF_INK_SOFT, fontWeight: here ? 600 : 400 }}>{s}</span>
            </div>
            {i < steps.length - 1 && <div style={{ flex: 1, borderBottom: `1px dashed ${WF_INK_FAINT}`, maxWidth: 40 }} />}
          </React.Fragment>
        );
      })}
      <div style={{ flex: 1 }} />
      <WFLabel>autosave on ✓</WFLabel>
    </div>
  );
}

function WizFooter({ left = '← Back', right = 'Continue →', primary = true, save = true }) {
  return (
    <div style={{ position: 'absolute', bottom: 0, left: 0, right: 0, padding: '12px 24px', borderTop: `1px dashed ${WF_INK_FAINT}`, display: 'flex', alignItems: 'center', gap: 10, background: WF_PAPER }}>
      <WFButton small>{left}</WFButton>
      <div style={{ flex: 1 }} />
      {save && <WFButton small>Save progress</WFButton>}
      <WFButton small primary={primary}>{right}</WFButton>
    </div>
  );
}

// ── O0 — Home entry (the banner + draft card) ───────────────
function FrameOnboardHome() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="New marina owner" scenario="Home · entry to wizard" />
      <WFAppBar active="My slips" />
      <div style={{ padding: 18 }}>
        {/* Setup banner — dismissible, shown when user has 0 marinas */}
        <WFCard accent style={{ background: WF_HIGHLIGHT, marginBottom: 14 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
            <div style={{ fontFamily: wfFontHand, fontSize: 26 }}>⚓</div>
            <div style={{ flex: 1 }}>
              <WFTitle level={2}>Set up your marina to get started</WFTitle>
              <WFNote style={{ fontSize: 12 }}>Profile → docks & slips → publish. We'll save your progress at every step. About 5 minutes.</WFNote>
            </div>
            <WFButton primary>Set up a marina →</WFButton>
            <WFButton small>Dismiss</WFButton>
          </div>
        </WFCard>

        <WFTitle level={1}>My marinas</WFTitle>
        <WFNote>1 draft · 0 published</WFNote>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12, marginTop: 12 }}>
          {/* Draft card — distinct variant */}
          <WFCard dashed style={{ background: '#fff' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div>
                <WFLabel>Draft · step 3 of 5</WFLabel>
                <WFTitle level={2} style={{ marginTop: 4 }}>Big Bay Marina</WFTitle>
                <WFNote style={{ fontSize: 11 }}>Annapolis, MD · 4 docks · ~80 slips planned</WFNote>
              </div>
              <WFPill bg={WF_HIGHLIGHT}>Draft</WFPill>
            </div>
            {/* progress bar */}
            <div style={{ marginTop: 10, height: 6, background: WF_PAPER_LINE, position: 'relative', filter: 'url(#wf-rough)' }}>
              <div style={{ width: '60%', height: '100%', background: WF_ACCENT }} />
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 10 }}>
              <WFButton small primary>Continue setup →</WFButton>
              <WFButton small>Delete draft</WFButton>
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 8 }}>Drafts are invisible to boaters · marketplace excluded</WFNote>
          </WFCard>

          {/* placeholder for future marinas */}
          <WFCard style={{ background: 'transparent', borderStyle: 'dashed', display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: 140 }}>
            <div style={{ textAlign: 'center', color: WF_INK_SOFT }}>
              <div style={{ fontFamily: wfFontHand, fontSize: 28 }}>+</div>
              <div style={{ fontSize: 12 }}>Add another marina</div>
              <WFNote style={{ fontSize: 10 }}>multi-marina tenants OK</WFNote>
            </div>
          </WFCard>

          <div style={{ visibility: 'hidden' }}></div>
        </div>

        <WFAnnotation rotate={-2} color={WF_BAD} style={{ bottom: 50, left: 30 }}>
          ↳ Banner hides forever after dismiss (localStorage).<br/>
          ↳ Draft card variant + "Continue setup" instead of "Open dashboard"
        </WFAnnotation>
      </div>
    </WFPage>
  );
}

// ── O1 — Step 1: Profile ────────────────────────────────────
function FrameWizardProfile() {
  return (
    <WFPage style={{ paddingBottom: 50 }}>
      <WFPersonaRibbon persona="Marina owner" scenario="Wizard · 1/5 · Profile" />
      <WFAppBar active="My slips" />
      <WizStepper current={1} />
      <div style={{ padding: '18px 32px', display: 'grid', gridTemplateColumns: '1fr 240px', gap: 24 }}>
        <div>
          <WFTitle level={1}>Tell us about your marina</WFTitle>
          <WFNote>We'll save this and create a draft marina right away — nothing is public until you publish.</WFNote>
          <div style={{ marginTop: 14, display: 'flex', flexDirection: 'column', gap: 10 }}>
            <WFInput label="Marina name" value="Big Bay Marina" big accent />
            <div>
              <WFLabel>Marina type</WFLabel>
              <div style={{ display: 'flex', gap: 6, marginTop: 5 }}>
                {[['Commercial', true], ['Yacht club'], ['Private community'], ['Municipal']].map(([t, sel]) => (
                  <WFCard key={t} accent={sel} style={{ padding: '6px 10px', cursor: 'pointer' }}>
                    <span style={{ fontSize: 12, fontWeight: sel ? 600 : 400 }}>{t}</span>
                  </WFCard>
                ))}
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
              <WFInput label="Contact email" value="ops@bigbaymarina.com" />
              <WFInput label="Phone" value="(410) 555-0123" />
            </div>
            <WFInput label="Public description (markdown OK)" value="Family-owned full-service marina with fuel dock, ship store, and 110 slips up to 65'…" />
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
              <WFInput label="Timezone" value="America/New_York" />
              <WFInput label="Currency" value="USD (MVP)" />
            </div>
          </div>
        </div>
        {/* Right rail: bookkeeping notes */}
        <div>
          <WFCard>
            <WFLabel>What's happening</WFLabel>
            <div style={{ fontSize: 11, marginTop: 6, lineHeight: 1.7 }}>
              ✓ Tenant created (Free)<br/>
              ✓ Marina created (draft)<br/>
              ✓ You = Owner Membership<br/>
              ○ Not yet visible to boaters
            </div>
          </WFCard>
          <WFAnnotation rotate={2} color={WF_BAD} style={{ position: 'static', marginTop: 14 }}>
            POST /marinas →<br/>
            IsSetupComplete=false<br/>
            SetupStep=1<br/>
            redirect /marina/{`{id}`}/setup
          </WFAnnotation>
        </div>
      </div>
      <WizFooter left="Cancel" right="Continue → location" />
    </WFPage>
  );
}

// ── O2 — Step 2: GPS location (geocode + map) ───────────────
function FrameWizardLocation() {
  return (
    <WFPage style={{ paddingBottom: 50 }}>
      <WFPersonaRibbon persona="Marina owner" scenario="Wizard · 2/5 · Location" />
      <WFAppBar active="My slips" />
      <WizStepper current={2} />
      <div style={{ padding: '18px 32px', display: 'grid', gridTemplateColumns: '1fr 320px', gap: 20 }}>
        <div>
          <WFTitle level={1}>Where is Big Bay Marina?</WFTitle>
          <WFNote>Type your address and click "Locate on map" — we'll drop a pin you can drag to fine-tune.</WFNote>
          <div style={{ marginTop: 12, display: 'flex', flexDirection: 'column', gap: 10 }}>
            <WFInput label="Street address" value="2 Boucher Ave" big accent />
            <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr', gap: 8 }}>
              <WFInput label="City" value="Annapolis" />
              <WFInput label="State" value="MD" />
              <WFInput label="ZIP" value="21403" />
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <WFButton primary>📍 Locate on map</WFButton>
              <WFButton small>Use current location</WFButton>
            </div>
            {/* Geocoder result banner — shows precision level */}
            <WFCard style={{ background: WF_ACCENT_SOFT, borderColor: WF_ACCENT }}>
              <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
                <div style={{ fontFamily: wfFontMono, fontSize: 11, color: WF_ACCENT, fontWeight: 700 }}>✓ FULL MATCH</div>
                <div style={{ fontSize: 11, flex: 1 }}>
                  Found <strong>2 Boucher Ave, Annapolis MD</strong> — drag the pin if the dock isn't exactly here.
                </div>
              </div>
            </WFCard>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
              <WFInput label="Latitude" value="38.9784" />
              <WFInput label="Longitude" value="-76.4922" />
            </div>
            <WFNote style={{ fontSize: 10 }}>Auto-updates when you drag the pin.</WFNote>
          </div>
        </div>
        <div>
          <WFMap style={{ width: '100%', height: 220 }} pins={1} label="leaflet · drag pin" />
          <WFAnnotation rotate={-2} color={WF_BAD} style={{ position: 'static', marginTop: 10 }}>
            Nominatim fallback chain:<br/>
            1. full address →<br/>
            2. city + state + zip →<br/>
            3. city + state →<br/>
            4. state only<br/>
            <br/>each level shows precision badge
          </WFAnnotation>
        </div>
      </div>
      <WizFooter right="Continue → docks & slips" />
    </WFPage>
  );
}

// ── O3 — Step 3: Dock & slip bulk builder ───────────────────
function FrameWizardBuilder() {
  return (
    <WFPage style={{ paddingBottom: 50 }}>
      <WFPersonaRibbon persona="Marina owner" scenario="Wizard · 3/5 · Docks & slips" />
      <WFAppBar active="My slips" />
      <WizStepper current={3} />
      <div style={{ padding: '14px 24px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 18 }}>
        {/* Left: dock builder */}
        <div>
          <WFTitle level={1}>Build your docks & slips</WFTitle>
          <WFNote>Generate hundreds of slips in seconds. Override individual slips on the next step.</WFNote>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Docks</WFLabel>
            <div style={{ display: 'flex', gap: 8, marginTop: 6, alignItems: 'flex-end' }}>
              <WFInput label="How many docks?" value="4" style={{ flex: 1 }} />
              <div style={{ flex: 1.4 }}>
                <WFLabel>Naming</WFLabel>
                <div style={{ display: 'flex', gap: 4, marginTop: 3 }}>
                  {[['Lettered', true], ['Numbered'], ['Manual']].map(([t, sel]) => (
                    <span key={t} style={{
                      padding: '4px 8px', fontSize: 11,
                      border: `1.5px solid ${sel ? WF_INK : WF_INK_FAINT}`,
                      background: sel ? WF_INK : 'transparent',
                      color: sel ? '#fff' : WF_INK,
                      filter: 'url(#wf-rough)',
                    }}>{t}</span>
                  ))}
                </div>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
              <WFInput label="Prefix" value="Dock " style={{ flex: 1 }} />
              <WFInput label="Suffix" value="" style={{ flex: 1 }} />
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 6 }}>Preview: Dock A · Dock B · Dock C · Dock D</WFNote>
          </WFCard>

          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Slip count</WFLabel>
            <div style={{ display: 'flex', gap: 6, marginTop: 5 }}>
              <span style={{ padding: '4px 8px', fontSize: 11, border: `1.5px solid ${WF_INK}`, background: WF_INK, color: '#fff', filter: 'url(#wf-rough)' }}>Same for all docks</span>
              <span style={{ padding: '4px 8px', fontSize: 11, border: `1.5px solid ${WF_INK_FAINT}`, color: WF_INK, filter: 'url(#wf-rough)' }}>Different per dock</span>
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 8, alignItems: 'flex-end' }}>
              <WFInput label="Slips per dock" value="20" style={{ flex: 1 }} />
              <div style={{ flex: 1.4 }}>
                <WFLabel>Slip naming</WFLabel>
                <div style={{ display: 'flex', gap: 4, marginTop: 3, flexWrap: 'wrap' }}>
                  {[['PerDockReset', true],['PerDockGlobal'],['Sequential'],['Manual']].map(([t, sel]) => (
                    <span key={t} style={{
                      padding: '4px 8px', fontSize: 10,
                      border: `1.5px solid ${sel ? WF_INK : WF_INK_FAINT}`,
                      background: sel ? WF_INK : 'transparent',
                      color: sel ? '#fff' : WF_INK,
                      fontFamily: wfFontMono,
                      filter: 'url(#wf-rough)',
                    }}>{t}</span>
                  ))}
                </div>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
              <WFInput label="Separator" value="-" style={{ width: 70 }} />
              <WFInput label="Start at" value="1" style={{ width: 80 }} />
              <WFCheckbox label="Pad zeros (01, 02…)" style={{ alignSelf: 'flex-end' }} />
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 6 }}>Preview: A-1 … A-20, B-1 … B-20, C-1 … C-20, D-1 … D-20 · <strong>80 slips total</strong></WFNote>
          </WFCard>
        </div>

        {/* Right: per-dock defaults (incl. new amenities) */}
        <div>
          <WFTitle level={2}>Dock-level defaults</WFTitle>
          <WFNote style={{ fontSize: 11 }}>Override on the preview step. Tabs let you set different defaults per dock.</WFNote>
          {/* dock tabs */}
          <div style={{ display: 'flex', gap: 4, marginTop: 8, borderBottom: `1.5px solid ${WF_INK}` }}>
            {['Dock A','Dock B','Dock C','Dock D'].map((d, i) => (
              <div key={d} style={{
                padding: '5px 10px', fontSize: 11, fontWeight: i===0 ? 600 : 400,
                background: i===0 ? WF_HIGHLIGHT : 'transparent',
                borderTop: `1px solid ${i===0 ? WF_INK : WF_INK_FAINT}`,
                borderLeft: `1px solid ${i===0 ? WF_INK : WF_INK_FAINT}`,
                borderRight: `1px solid ${i===0 ? WF_INK : WF_INK_FAINT}`,
                marginBottom: -1,
              }}>{d}</div>
            ))}
          </div>
          <WFCard style={{ marginTop: 0, borderTop: 'none' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8 }}>
              <WFInput label="Max length" value="42'" />
              <WFInput label="Max beam" value="14'" />
              <WFInput label="Max draft" value="6'" />
            </div>
            <div style={{ marginTop: 8 }}>
              <WFLabel>Slip type</WFLabel>
              <div style={{ display: 'flex', gap: 4, marginTop: 3 }}>
                {[['Floating', true],['Fixed'],['Mooring']].map(([t,sel]) => (
                  <span key={t} style={{ padding: '3px 8px', fontSize: 10, border: `1.5px solid ${sel ? WF_INK : WF_INK_FAINT}`, background: sel ? WF_INK : 'transparent', color: sel ? '#fff' : WF_INK, filter: 'url(#wf-rough)' }}>{t}</span>
                ))}
              </div>
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <WFLabel>Amenities</WFLabel>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4, marginTop: 5 }}>
              <WFCheckbox label="Electric" checked />
              <div style={{ display: 'flex', gap: 4, alignItems: 'center', fontSize: 11 }}>
                <span style={{ padding: '2px 6px', fontSize: 9, fontFamily: wfFontMono, border: `1px solid ${WF_INK}`, background: WF_INK, color: '#fff' }}>50A</span>
                <span style={{ padding: '2px 6px', fontSize: 9, fontFamily: wfFontMono, border: `1px solid ${WF_INK_FAINT}` }}>30A</span>
                <span style={{ padding: '2px 6px', fontSize: 9, fontFamily: wfFontMono, border: `1px solid ${WF_INK_FAINT}` }}>none</span>
              </div>
              <WFCheckbox label="Water" checked />
              <WFCheckbox label="Pump-out" checked />
              <WFCheckbox label="Covered" />
              <WFCheckbox label="Indoor" />
            </div>
            <WFLabel style={{ marginTop: 10 }}>Custom tags (Amenities[])</WFLabel>
            <div style={{ display: 'flex', gap: 4, marginTop: 4, flexWrap: 'wrap', alignItems: 'center' }}>
              <WFTag>Fuel dock ×</WFTag>
              <WFTag>Wi-Fi ×</WFTag>
              <WFTag>Restaurant access ×</WFTag>
              <span style={{ fontSize: 10, color: WF_INK_FAINT, fontFamily: wfFontMono, border: `1px dashed ${WF_INK_FAINT}`, padding: '2px 6px' }}>+ add tag</span>
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 8 }}>Custom tags shown on slip detail. Not search-filterable in MVP.</WFNote>
          </WFCard>
          <WFAnnotation rotate={2} color={WF_BAD} style={{ position: 'static', marginTop: 8, fontSize: 11 }}>
            extensible generator: generateSlipName(convention, dockIdx, slipIdx, …)
          </WFAnnotation>
        </div>
      </div>
      <WizFooter right="Continue → preview & adjust" />
    </WFPage>
  );
}

// ── O4 — Step 4: Preview & adjust ───────────────────────────
function FrameWizardPreview() {
  return (
    <WFPage style={{ paddingBottom: 50 }}>
      <WFPersonaRibbon persona="Marina owner" scenario="Wizard · 4/5 · Preview & adjust" />
      <WFAppBar active="My slips" />
      <WizStepper current={4} />
      <div style={{ padding: '14px 24px', height: 'calc(100% - 90px - 50px)', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
          <div>
            <WFTitle level={1}>Preview & adjust</WFTitle>
            <WFNote>4 docks · 80 slips · click any cell to edit · use "Bulk edit" to change all slips in a dock at once</WFNote>
          </div>
          <div style={{ display: 'flex', gap: 6 }}>
            <WFButton small>← Back to builder</WFButton>
            <WFButton small>Add a dock</WFButton>
          </div>
        </div>

        {/* Table */}
        <WFCard padding={0} style={{ marginTop: 10, overflow: 'auto', flex: 1 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '60px 1.1fr 90px 90px 60px 110px 110px 60px', padding: '6px 10px', fontFamily: wfFontMono, fontSize: 9, textTransform: 'uppercase', letterSpacing: 0.6, color: WF_INK_SOFT, borderBottom: `1px dashed ${WF_INK_FAINT}`, position: 'sticky', top: 0, background: WF_PAPER }}>
            <span></span><span>Slip</span><span>Max L×B×D</span><span>Type</span><span>Elec</span><span>Water/Pump/Cov</span><span>Custom tags</span><span></span>
          </div>

          {/* Dock A — expanded */}
          <div style={{ display: 'grid', gridTemplateColumns: '24px 1fr auto', padding: '6px 10px', background: WF_HIGHLIGHT, alignItems: 'center', borderBottom: `1.5px solid ${WF_INK}` }}>
            <span style={{ fontFamily: wfFontMono, fontSize: 11 }}>▾</span>
            <strong style={{ fontSize: 13 }}>Dock A · 20 slips · 50A · pump-out</strong>
            <div style={{ display: 'flex', gap: 4 }}>
              <WFButton small>Bulk edit</WFButton>
              <WFButton small>+ Slip</WFButton>
              <WFButton small>Remove dock</WFButton>
            </div>
          </div>
          {[
            ['A-1','42×14×6','Floating','50A','✓✓✗','Fuel dock, Wi-Fi'],
            ['A-2','42×14×6','Floating','50A','✓✓✗','Fuel dock, Wi-Fi'],
            ['A-3','60×16×7','Floating','50A','✓✓✓','Fuel dock, Wi-Fi, Pier-end', true],
            ['A-4','42×14×6','Floating','50A','✓✓✗','Fuel dock, Wi-Fi'],
            ['A-5','42×14×6','Floating','50A','✓✓✗','Fuel dock, Wi-Fi'],
          ].map((r, i) => (
            <div key={r[0]} style={{ display: 'grid', gridTemplateColumns: '60px 1.1fr 90px 90px 60px 110px 110px 60px', padding: '6px 10px', borderBottom: `1px dashed ${WF_INK_FAINT}`, fontSize: 11, alignItems: 'center', background: r[6] ? WF_ACCENT_SOFT : undefined }}>
              <span></span>
              <strong style={{ fontFamily: wfFontMono }}>{r[0]} {r[6] && <WFTag style={{ marginLeft: 4, fontSize: 9 }}>OVERRIDE</WFTag>}</strong>
              <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[1]}</span>
              <span>{r[2]}</span>
              <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[3]}</span>
              <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[4]}</span>
              <span style={{ fontSize: 10, color: WF_INK_SOFT, overflow: 'hidden', whiteSpace: 'nowrap', textOverflow: 'ellipsis' }}>{r[5]}</span>
              <span style={{ textAlign: 'right', color: WF_INK_FAINT, fontFamily: wfFontMono, fontSize: 11 }}>···</span>
            </div>
          ))}
          <div style={{ padding: '6px 10px', fontSize: 11, color: WF_INK_SOFT, fontStyle: 'italic', borderBottom: `1px dashed ${WF_INK_FAINT}` }}>
            …15 more slips in Dock A
          </div>

          {/* Other docks — collapsed */}
          {[['B', 20, '50A · covered'], ['C', 20, '30A'], ['D', 20, '30A · mooring']].map(([d, n, summary]) => (
            <div key={d} style={{ display: 'grid', gridTemplateColumns: '24px 1fr auto', padding: '6px 10px', background: WF_PAPER_LINE, alignItems: 'center', borderBottom: `1px dashed ${WF_INK_FAINT}` }}>
              <span style={{ fontFamily: wfFontMono, fontSize: 11 }}>▸</span>
              <strong style={{ fontSize: 12 }}>Dock {d} · {n} slips · {summary}</strong>
              <span style={{ fontFamily: wfFontMono, fontSize: 10, color: WF_INK_SOFT }}>click to expand</span>
            </div>
          ))}
        </WFCard>

        <WFAnnotation rotate={-1} color={WF_BAD} style={{ position: 'static', marginTop: 8 }}>
          Saved via PUT /marinas/{`{id}`}/setup/docks (atomic replace) · debounced + on-blur · OVERRIDE badge marks slips diverged from dock defaults
        </WFAnnotation>
      </div>
      <WizFooter right="Continue → publish" />
    </WFPage>
  );
}

// ── O5 — Step 5: Publish ────────────────────────────────────
function FrameWizardPublish() {
  return (
    <WFPage style={{ paddingBottom: 50 }}>
      <WFPersonaRibbon persona="Marina owner" scenario="Wizard · 5/5 · Publish" />
      <WFAppBar active="My slips" />
      <WizStepper current={5} />
      <div style={{ padding: '20px 32px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 22 }}>
        <div>
          <WFTitle level={1}>Ready to go live?</WFTitle>
          <WFNote>Review your setup. You can always add docks, slips, photos, and listings later.</WFNote>
          <WFCard style={{ marginTop: 14 }}>
            <WFLabel>Summary</WFLabel>
            <div style={{ marginTop: 8, fontSize: 12, lineHeight: 1.9 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Name</span><strong>Big Bay Marina</strong></div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Type</span><strong>Commercial</strong></div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Location</span><strong>Annapolis, MD</strong></div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Docks</span><strong>4</strong></div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>Slips</span><strong>80</strong></div>
              <div style={{ display: 'flex', justifyContent: 'space-between', color: WF_INK_SOFT }}><span>Photos</span><span>0 · add later</span></div>
              <div style={{ display: 'flex', justifyContent: 'space-between', color: WF_INK_SOFT }}><span>Listings</span><span>0 · add per slip</span></div>
            </div>
          </WFCard>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Next up</WFLabel>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>
              · Upload photos (slip + marina)<br/>
              · Create your first AvailabilityWindow<br/>
              · Invite staff<br/>
              · Import your customer list (BillingAccounts)
            </WFNote>
          </WFCard>
        </div>

        <div>
          <WFCard accent>
            <WFLabel>Publish options</WFLabel>
            <WFTitle level={2} style={{ marginTop: 4 }}>Activate your marina</WFTitle>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>Required to start managing slips, invoicing, and staff.</WFNote>
            <WFLine dashed style={{ margin: '12px 0' }} />
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 10 }}>
              <div>
                <div style={{ fontWeight: 600, fontSize: 13 }}>List on the marketplace</div>
                <WFNote style={{ fontSize: 11, marginTop: 3 }}>
                  Slips with open AvailabilityWindows become bookable by boaters. You can list later from any slip — not required to activate.
                </WFNote>
              </div>
              <WFToggle on={false} />
            </div>
            <WFNote style={{ fontSize: 10, marginTop: 8, color: WF_BAD }}>Defaults OFF · explicit opt-in required</WFNote>
            <WFLine dashed style={{ margin: '12px 0' }} />
            <WFButton primary style={{ width: '100%' }}>Activate marina</WFButton>
            <WFNote style={{ fontSize: 10, textAlign: 'center', marginTop: 6 }}>Sets IsSetupComplete = true · IsListed = toggle</WFNote>
          </WFCard>
          <WFAnnotation rotate={2} color={WF_BAD} style={{ position: 'static', marginTop: 14 }}>
            ★ Explicit publish gate — drafts NEVER hit the marketplace
          </WFAnnotation>
        </div>
      </div>
      <WizFooter right="Activate marina →" />
    </WFPage>
  );
}

Object.assign(window, {
  FrameOnboardHome, FrameWizardProfile, FrameWizardLocation,
  FrameWizardBuilder, FrameWizardPreview, FrameWizardPublish,
});
