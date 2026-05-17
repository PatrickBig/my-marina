// Boater-side wireframes:
// B1 search (marina rollup · step 1) · B1b slips-at-marina (step 2)
// B2 slip detail · B3 booking · B4 dashboard · B5 my-slip + I'm Away
// All frames assume 720x500.

// B1 — Marina rollup (step 1) · viewport-bounded, marina-first
function FrameSearch() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Boater" scenario="Search · step 1 · marinas" />
      <WFAppBar active="Find a slip" />
      {/* control bar — no radius, no "Where" text input. The map IS the search area. */}
      <div style={{ padding: '10px 18px', borderBottom: `1px dashed ${WF_INK_FAINT}`, display: 'flex', gap: 10, alignItems: 'flex-end' }}>
        <div style={{ flex: 1.4 }}>
          <WFLabel>Boat</WFLabel>
          <WFBox style={{ height: 28, display: 'flex', alignItems: 'center', padding: '0 10px', background: '#fff', marginTop: 3 }}>
            <span style={{ fontFamily: wfFont, fontSize: 12, flex: 1 }}>Wanderlust · 38'×12'9"×6' ▾</span>
          </WFBox>
        </div>
        <WFInput label="Arrive" value="Aug 5" style={{ flex: 1 }} />
        <WFInput label="Depart" value="Aug 8" style={{ flex: 1 }} />
        <div style={{ flex: 0.8 }}>
          <WFLabel>Kind</WFLabel>
          <WFBox style={{ height: 28, display: 'flex', alignItems: 'center', padding: '0 10px', background: '#fff', marginTop: 3 }}>
            <span style={{ fontFamily: wfFont, fontSize: 12 }}>Transient ▾</span>
          </WFBox>
        </div>
      </div>
      {/* filter chips */}
      <div style={{ padding: '7px 18px', display: 'flex', gap: 8, borderBottom: `1px dashed ${WF_INK_FAINT}`, alignItems: 'center' }}>
        <WFPill>Instant book</WFPill>
        <WFPill>Electric</WFPill>
        <WFPill>Pump-out</WFPill>
        <WFPill>Floating</WFPill>
        <WFPill color={WF_INK_SOFT}>+ filter</WFPill>
        <div style={{ flex: 1 }} />
        <WFLabel>Sort: Most options ▾</WFLabel>
      </div>
      {/* split: rollup list + map */}
      <div style={{ display: 'flex', height: 'calc(100% - 126px)' }}>
        <div style={{ width: 320, padding: '10px 14px', overflow: 'hidden', display: 'flex', flexDirection: 'column', gap: 7, borderRight: `1px dashed ${WF_INK_FAINT}` }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <WFLabel>4 marinas in view · 23 slips fit</WFLabel>
            <WFNote style={{ fontSize: 10 }}>step 1 of 2</WFNote>
          </div>
          {[
            { name: 'Big Bay Marina', loc: 'Annapolis, MD', avail: 6, rate: 'PerFoot', priceLabel: '$4.20–$5.80 /ft', inst: true, dist: '0.4mi', accent: true },
            { name: 'Eastport Yacht Club', loc: 'Annapolis, MD', avail: 3, rate: 'Flat', priceLabel: '$95–$180 /nt', inst: false, dist: '1.4mi' },
            { name: "Pat's dock", loc: 'Solomons, MD', avail: 1, rate: 'Flat', priceLabel: '$95 /nt', inst: true, dist: '12.2mi' },
            { name: 'Tidewater Marina', loc: 'Kent Island, MD', avail: 13, rate: 'Mixed', priceLabel: 'from $110 /nt', inst: true, dist: '8.7mi' },
          ].map((r, i) => (
            <WFCard key={i} accent={r.accent} style={{ height: 86 }}>
              <div style={{ display: 'flex', gap: 10, height: '100%' }}>
                <WFPlaceholder label="marina photo" height={64} style={{ width: 72, flexShrink: 0 }} />
                <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                    <div style={{ fontWeight: 600 }}>{r.name}</div>
                    <div style={{ fontFamily: wfFontMono, fontSize: 9, color: WF_INK_SOFT }}>{r.dist}</div>
                  </div>
                  <WFNote style={{ fontSize: 11 }}>{r.loc} · <strong>{r.avail}</strong> slip{r.avail===1?'':'s'} fit</WFNote>
                  <div style={{ fontFamily: wfFontMono, fontSize: 11, color: WF_INK }}>{r.priceLabel}</div>
                  <div style={{ display: 'flex', gap: 5, marginTop: 'auto' }}>
                    {r.inst && <WFTag>Instant</WFTag>}
                    <WFTag style={{ background: WF_PAPER_LINE }}>{r.rate}</WFTag>
                    <span style={{ marginLeft: 'auto', fontFamily: wfFontMono, fontSize: 10, color: WF_ACCENT }}>view slips →</span>
                  </div>
                </div>
              </div>
            </WFCard>
          ))}
        </div>
        <div style={{ flex: 1, padding: 10, position: 'relative' }}>
          <WFMap style={{ width: '100%', height: '100%' }} pins={6} label="leaflet · viewport = search area" />
          {/* Zillow-pattern: Search this area button surfaces on pan/zoom */}
          <div style={{ position: 'absolute', top: 18, left: '50%', transform: 'translateX(-50%)' }}>
            <WFButton primary small style={{ background: '#fff', color: WF_INK, border: `1.5px solid ${WF_INK}`, boxShadow: '0 2px 0 rgba(0,0,0,.2)' }}>
              ↻ Search this area
            </WFButton>
          </div>
          <WFAnnotation rotate={-3} color={WF_BAD} style={{ top: 60, left: 30 }}>
            ↳ map viewport bbox = search box.<br/>No radius input.
          </WFAnnotation>
          <WFAnnotation rotate={3} color={WF_BAD} style={{ bottom: 30, right: 30 }}>
            pins = marinas (not slips)<br/>rollup count overlaid
          </WFAnnotation>
        </div>
      </div>
    </WFPage>
  );
}

// B1b — Slips at a marina (step 2)
function FrameSearchMarina() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Boater" scenario="Search · step 2 · slips at marina" />
      <WFAppBar active="Find a slip" />
      <div style={{ padding: '8px 18px', borderBottom: `1px dashed ${WF_INK_FAINT}`, display: 'flex', alignItems: 'center', gap: 10 }}>
        <WFButton small>← Back to marinas</WFButton>
        <div style={{ flex: 1 }}>
          <WFTitle level={2}>Big Bay Marina</WFTitle>
          <WFNote style={{ fontSize: 11 }}>Annapolis, MD · Aug 5 → Aug 8 · Wanderlust 38'</WFNote>
        </div>
        <WFPill bg={WF_HIGHLIGHT}>6 slips fit</WFPill>
      </div>
      <div style={{ display: 'flex', height: 'calc(100% - 96px)' }}>
        <div style={{ width: 360, padding: '10px 14px', overflow: 'hidden', display: 'flex', flexDirection: 'column', gap: 7, borderRight: `1px dashed ${WF_INK_FAINT}` }}>
          <div style={{ display: 'flex', gap: 6, marginBottom: 2 }}>
            <WFPill>Instant book 4</WFPill>
            <WFPill>50A 5</WFPill>
            <WFPill>Covered 1</WFPill>
            <div style={{ flex: 1 }} />
            <WFLabel>Sort: price ▾</WFLabel>
          </div>
          {[
            { id: 'A-12', dim: "42'×14'×6'", price: '$148/nt', inst: true, am: ['50A','Water','Pump-out'], accent: true },
            { id: 'A-18', dim: "50'×16'×7'", price: '$210/nt', inst: true, am: ['50A','Water'] },
            { id: 'B-3',  dim: "40'×14'×5'", price: '$132/nt', inst: false, am: ['30A','Water'] },
            { id: 'B-7',  dim: "42'×14'×6'", price: '$148/nt', inst: true, am: ['50A','Covered'] },
            { id: 'C-2',  dim: "38'×12'×5'", price: '$118/nt', inst: false, am: ['30A','Pump-out'] },
          ].map((s, i) => (
            <WFCard key={s.id} accent={s.accent}>
              <div style={{ display: 'flex', gap: 10 }}>
                <WFPlaceholder label="slip" height={56} style={{ width: 80, flexShrink: 0 }} />
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <div style={{ fontWeight: 600 }}>Slip {s.id}</div>
                    <div style={{ fontFamily: wfFontMono, fontSize: 11 }}>{s.price}</div>
                  </div>
                  <WFNote style={{ fontSize: 11 }}>Floating · max {s.dim}</WFNote>
                  <div style={{ display: 'flex', gap: 4, marginTop: 4, flexWrap: 'wrap' }}>
                    {s.inst && <WFTag>Instant</WFTag>}
                    {s.am.map(a => <WFTag key={a} style={{ background: WF_PAPER_LINE }}>{a}</WFTag>)}
                  </div>
                </div>
              </div>
            </WFCard>
          ))}
        </div>
        <div style={{ flex: 1, padding: 10, position: 'relative' }}>
          <WFMap style={{ width: '100%', height: '100%' }} pins={1} label="marina pin (single)" />
          <WFAnnotation rotate={-2} color={WF_BAD} style={{ top: 50, right: 30 }}>
            single pin · per-slip viz<br/>deferred (marina-map-viz change)
          </WFAnnotation>
          <div style={{ position: 'absolute', bottom: 14, left: 14, right: 14, padding: 8, background: WF_PAPER, border: `1.5px solid ${WF_INK}`, filter: 'url(#wf-rough)' }}>
            <WFLabel>About Big Bay Marina</WFLabel>
            <WFNote style={{ fontSize: 11, marginTop: 3 }}>Family-owned since '78 · fuel dock, ship store, restaurant on-site · gate code emailed at booking.</WFNote>
          </div>
        </div>
      </div>
    </WFPage>
  );
}

// B2 — Slip detail
function FrameSlipDetail() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Boater" scenario="Slip detail" />
      <WFAppBar active="Find a slip" />
      <div style={{ padding: 14, display: 'grid', gridTemplateColumns: '1fr 260px', gap: 14, height: 'calc(100% - 40px)' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10, overflow: 'hidden' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr', gap: 6, height: 130 }}>
            <WFPlaceholder label="hero · slip from water" height="100%" />
            <div style={{ display: 'grid', gridTemplateRows: '1fr 1fr', gap: 6 }}>
              <WFPlaceholder label="aerial" height="100%" />
              <WFPlaceholder label="dock" height="100%" />
            </div>
            <div style={{ display: 'grid', gridTemplateRows: '1fr 1fr', gap: 6 }}>
              <WFPlaceholder label="amenities" height="100%" />
              <WFPlaceholder label="+5 more" height="100%" />
            </div>
          </div>
          <div>
            <WFTitle level={1}>Big Bay Marina · A-12</WFTitle>
            <WFNote>Annapolis, MD · hosted by <WFUnderline>Big Bay Marina</WFUnderline> · 4.8 ★ (post-MVP)</WFNote>
          </div>
          <WFLine dashed />
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 8, fontSize: 11 }}>
            {[['Max length', "42'"], ['Max beam', "14'"], ['Max draft', "6'"], ['Type', 'Floating']].map(([k, v]) => (
              <div key={k}>
                <WFLabel>{k}</WFLabel>
                <div style={{ fontFamily: wfFontMono, fontSize: 13, marginTop: 2 }}>{v}</div>
              </div>
            ))}
          </div>
          <div>
            <WFLabel>Core amenities</WFLabel>
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginTop: 4 }}>
              {['50A electric', 'Water', 'Pump-out', 'Covered'].map(a => <WFTag key={a}>{a}</WFTag>)}
              <WFTag style={{ background: WF_PAPER_LINE, color: WF_INK_FAINT, textDecoration: 'line-through' }}>Indoor</WFTag>
            </div>
            <WFLabel style={{ marginTop: 6 }}>Marina tags</WFLabel>
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginTop: 4 }}>
              {['Wi-Fi', 'Restrooms & showers', 'Fuel dock', 'Ice', 'Restaurant on-site'].map(a => <WFTag key={a} style={{ background: WF_PAPER_LINE }}>{a}</WFTag>)}
            </div>
          </div>
          <WFLine dashed />
          <div>
            <WFLabel>About this slip</WFLabel>
            <WFNote style={{ marginTop: 4, fontSize: 12 }}>
              Quiet end-tie on A-dock with easy in/out. 50A pedestal + water at the post. Gate code emailed at booking. Restrooms + showers on the pier; chandlery and pump-out 200ft.
            </WFNote>
          </div>
          <div>
            <WFLabel>Cancellation</WFLabel>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>Free up to 7 days before arrival · 50% within 7d · none within 24h</WFNote>
          </div>
        </div>
        {/* booking card */}
        <div style={{ position: 'sticky', top: 0 }}>
          <WFCard accent>
            <WFTitle level={2}>$148<span style={{ fontSize: 12, fontWeight: 400, color: WF_INK_SOFT }}> /night</span></WFTitle>
            <WFLabel style={{ marginTop: 2 }}>weekly −10% · monthly −20%</WFLabel>
            <div style={{ marginTop: 10, display: 'flex', flexDirection: 'column', gap: 6 }}>
              <WFInput label="Arrive" value="Aug 5 · 3pm" />
              <WFInput label="Depart" value="Aug 8 · 11am" />
              <WFInput label="Boat" value="Wanderlust 38' Catalina" />
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11 }}><span>$148 × 3 nts</span><span>$444</span></div>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11 }}><span>Cleaning</span><span>$25</span></div>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, fontWeight: 600, marginTop: 4 }}><span>Total</span><span>$469</span></div>
            <WFNote style={{ fontSize: 10, marginTop: 4 }}>Era 1 · marina invoices you direct. No card charge today.</WFNote>
            <WFButton primary style={{ width: '100%', marginTop: 10 }}>Instant book</WFButton>
            <div style={{ display: 'flex', gap: 4, marginTop: 6, justifyContent: 'center' }}>
              <WFTag>Instant book</WFTag>
              <WFTag style={{ background: WF_PAPER_LINE }}>Off-platform pay</WFTag>
            </div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

// B3 — Reservation review (modal-ish full screen)
function FrameBookingFlow() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Boater" scenario="Confirm reservation" />
      <WFAppBar active="Find a slip" />
      <div style={{ padding: 18, display: 'flex', justifyContent: 'center' }}>
        <div style={{ width: 540 }}>
          <WFTitle level={1}>Confirm your reservation</WFTitle>
          <WFNote>Step 2 of 2 · review &amp; submit</WFNote>
          <WFCard style={{ marginTop: 14 }}>
            <div style={{ display: 'flex', gap: 12 }}>
              <WFPlaceholder label="slip" height={70} style={{ width: 100, flexShrink: 0 }} />
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600 }}>Big Bay Marina · A-12</div>
                <WFNote style={{ fontSize: 11 }}>Aug 5 → Aug 8 · 3 nights · Wanderlust</WFNote>
                <div style={{ display: 'flex', gap: 5, marginTop: 5 }}><WFTag>Instant book</WFTag><WFTag style={{ background: WF_PAPER_LINE }}>Confirms now</WFTag></div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontFamily: wfFontMono, fontSize: 14 }}>$469</div>
                <WFNote style={{ fontSize: 10 }}>tax 0% · MVP</WFNote>
              </div>
            </div>
          </WFCard>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Vessel for this trip</WFLabel>
            <div style={{ display: 'flex', gap: 8, marginTop: 6 }}>
              <WFCard accent style={{ flex: 1, padding: 8 }}>
                <div style={{ fontWeight: 600, fontSize: 12 }}>Wanderlust</div>
                <WFNote style={{ fontSize: 10 }}>Catalina 38 · 38' × 12'9" × 6'</WFNote>
                <WFTag style={{ marginTop: 4 }}>FITS</WFTag>
              </WFCard>
              <WFCard style={{ flex: 1, padding: 8 }}>
                <div style={{ fontSize: 12 }}>+ Add a different boat</div>
              </WFCard>
            </div>
          </WFCard>
          <WFCard style={{ marginTop: 10 }}>
            <WFLabel>Note to host (optional)</WFLabel>
            <WFBox style={{ marginTop: 5, height: 50, padding: 8, background: '#fff' }}>
              <WFNote style={{ fontSize: 11 }}>Arriving from the south around 2pm. We'll hail on 16 once we round the point. Thanks!</WFNote>
            </WFBox>
          </WFCard>
          <WFCard style={{ marginTop: 10 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <div>
                <div style={{ fontWeight: 600, fontSize: 12 }}>Payment</div>
                <WFNote style={{ fontSize: 10 }}>Off-platform · marina will invoice you · MVP</WFNote>
              </div>
              <WFTag>Era 1</WFTag>
            </div>
          </WFCard>
          <div style={{ display: 'flex', gap: 10, marginTop: 14, justifyContent: 'flex-end' }}>
            <WFButton>Back</WFButton>
            <WFButton primary>Submit reservation</WFButton>
          </div>
          <WFAnnotation style={{ top: 380, left: -160 }}>
            ← state machine entry:<br />Confirmed (instant) /<br />PendingApproval (req) /<br />PendingHostMarinaApproval
          </WFAnnotation>
        </div>
      </div>
    </WFPage>
  );
}

// B4 — Multi-marina dashboard
function FrameBoaterDashboard() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Boater + Host" scenario="Unified dashboard" />
      <WFAppBar active="My trips" />
      <div style={{ padding: 16, display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 14, height: 'calc(100% - 40px)', overflow: 'hidden' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10, overflow: 'hidden' }}>
          <div>
            <WFTitle level={1}>Welcome back, Maria</WFTitle>
            <WFNote>3 marinas · 2 boats · 1 slip you host</WFNote>
          </div>
          {/* Upcoming */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
              <WFTitle level={2}>Upcoming reservations</WFTitle>
              <WFLabel>across all marinas</WFLabel>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 7, marginTop: 6 }}>
              {[
                { d: 'Aug 5–8', s: 'Big Bay Marina · A-12', tag: 'Confirmed', t: WF_GOOD },
                { d: 'Sep 2–4', s: "Pat's dock · Solomons", tag: 'Pending host', t: WF_INK_SOFT },
              ].map((r, i) => (
                <WFCard key={i}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                      <div style={{ fontWeight: 600 }}>{r.s}</div>
                      <WFNote style={{ fontSize: 11 }}>{r.d} · Wanderlust</WFNote>
                    </div>
                    <WFPill color={r.t}>{r.tag}</WFPill>
                  </div>
                </WFCard>
              ))}
            </div>
          </div>
          {/* My slip (long-term) */}
          <div>
            <WFTitle level={2}>My slip · Eastport YC</WFTitle>
            <WFCard accent style={{ marginTop: 6 }}>
              <div style={{ display: 'flex', gap: 12 }}>
                <WFPlaceholder label="slip" height={56} style={{ width: 80 }} />
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600 }}>Slip C-7 · Eastport YC</div>
                  <WFNote style={{ fontSize: 11 }}>Seasonal · Apr 15 → Oct 31 · $640/mo</WFNote>
                  <div style={{ display: 'flex', gap: 5, marginTop: 4 }}>
                    <WFTag>Sublet allowed</WFTag>
                    <WFTag style={{ background: WF_PAPER_LINE }}>Owner sublet 70/30</WFTag>
                  </div>
                </div>
                <WFButton small>I'll be away</WFButton>
              </div>
            </WFCard>
          </div>
          {/* Hosting (the cross-role part) */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
              <WFTitle level={2}>Slips you host</WFTitle>
              <WFLabel>same login · no toggle</WFLabel>
            </div>
            <WFCard style={{ marginTop: 6 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <div style={{ fontWeight: 600 }}>Maria's slip at Big Bay</div>
                  <WFNote style={{ fontSize: 11 }}>Dockominium · 4 upcoming bookings · $1,820 earned this season</WFNote>
                </div>
                <WFButton small>Manage →</WFButton>
              </div>
            </WFCard>
          </div>
        </div>
        {/* Right rail */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <WFCard>
            <WFLabel>Outstanding invoices</WFLabel>
            <div style={{ fontFamily: wfFontMono, fontSize: 20, marginTop: 4 }}>$1,247.50</div>
            <WFNote style={{ fontSize: 11 }}>2 marinas · Big Bay $980, Eastport $267</WFNote>
            <WFButton small style={{ marginTop: 6 }}>View invoices</WFButton>
          </WFCard>
          <WFCard>
            <WFLabel>Open requests</WFLabel>
            <div style={{ fontSize: 12, marginTop: 5 }}>· Bilge pump replacement <WFTag>In progress</WFTag></div>
            <div style={{ fontSize: 12, marginTop: 4 }}>· Pedestal flickering <WFTag style={{ background: WF_PAPER_LINE }}>Submitted</WFTag></div>
          </WFCard>
          <WFCard>
            <WFLabel>Announcements</WFLabel>
            <WFNote style={{ fontSize: 11, marginTop: 5 }}>📌 Big Bay · "Fuel dock closed Mon morning"</WFNote>
            <WFNote style={{ fontSize: 11, marginTop: 4 }}>· Eastport · "Annual haul-out signups open"</WFNote>
          </WFCard>
          <WFCard>
            <WFLabel>My boats</WFLabel>
            <div style={{ display: 'flex', gap: 6, marginTop: 6 }}>
              <WFCard style={{ flex: 1, padding: 8 }}>
                <div style={{ fontSize: 12, fontWeight: 600 }}>Wanderlust</div>
                <WFNote style={{ fontSize: 10 }}>Catalina 38</WFNote>
              </WFCard>
              <WFCard style={{ flex: 1, padding: 8 }}>
                <div style={{ fontSize: 12, fontWeight: 600 }}>Skiff</div>
                <WFNote style={{ fontSize: 10 }}>13' Whaler</WFNote>
              </WFCard>
            </div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

// B5 — My slip detail + I'm Away flow
function FrameMySlipAway() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Boater" scenario="My slip · I'm away" />
      <WFAppBar active="My slips" />
      <div style={{ padding: 16, display: 'grid', gridTemplateColumns: '1fr 320px', gap: 14, height: 'calc(100% - 40px)', overflow: 'hidden' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <WFTitle level={1}>Slip C-7</WFTitle>
            <WFNote>· Eastport YC · seasonal lease</WFNote>
          </div>
          <div style={{ display: 'flex', gap: 6, marginTop: 4 }}>
            <WFTag>Apr 15 → Oct 31</WFTag>
            <WFTag style={{ background: WF_PAPER_LINE }}>$640/mo</WFTag>
            <WFTag style={{ background: WF_PAPER_LINE }}>Wanderlust</WFTag>
          </div>
          <WFCard style={{ marginTop: 12 }}>
            <WFLabel>Lease policy</WFLabel>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginTop: 8, fontSize: 12 }}>
              <div><WFToggle on /> Marina may sublet when I'm away</div>
              <div><WFToggle on /> I may sublet myself</div>
              <div>Owner-sublet share to me: <strong>30%</strong></div>
              <div>My-sublet share to marina: <strong>15%</strong></div>
            </div>
            <WFNote style={{ fontSize: 11, marginTop: 8 }}>Set at lease signing · contact marina to renegotiate</WFNote>
          </WFCard>
          {/* timeline */}
          <div style={{ marginTop: 14 }}>
            <WFTitle level={2}>Calendar</WFTitle>
            <div style={{ marginTop: 8, height: 70, position: 'relative', border: `1.5px solid ${WF_INK}`, filter: 'url(#wf-rough)' }}>
              {['Apr','May','Jun','Jul','Aug','Sep','Oct'].map((m, i) => (
                <div key={m} style={{ position: 'absolute', left: `${(i / 7) * 100}%`, top: 0, bottom: 0, borderLeft: i ? `1px dashed ${WF_INK_FAINT}` : 'none', width: `${100/7}%`, fontSize: 9, fontFamily: wfFontMono, padding: 2 }}>{m}</div>
              ))}
              {/* my use */}
              <div style={{ position: 'absolute', top: 22, left: '5%', width: '20%', height: 14, background: WF_ACCENT_SOFT, border: `1px solid ${WF_ACCENT}`, fontSize: 9, padding: '0 4px' }}>me</div>
              {/* away window */}
              <div style={{ position: 'absolute', top: 22, left: '32%', width: '12%', height: 14, background: WF_HIGHLIGHT, border: `1px solid ${WF_INK}`, fontSize: 9, padding: '0 4px', fontFamily: wfFontMono }}>AWAY · sublet</div>
              {/* my use */}
              <div style={{ position: 'absolute', top: 22, left: '50%', width: '40%', height: 14, background: WF_ACCENT_SOFT, border: `1px solid ${WF_ACCENT}`, fontSize: 9, padding: '0 4px' }}>me</div>
              {/* sublet booked */}
              <div style={{ position: 'absolute', top: 40, left: '34%', width: '8%', height: 14, background: WF_INK, color: '#fff', fontSize: 9, padding: '0 4px', fontFamily: wfFontMono }}>booked</div>
            </div>
          </div>
          <div style={{ marginTop: 14 }}>
            <WFTitle level={2}>Sublet earnings</WFTitle>
            <div style={{ display: 'flex', gap: 10, marginTop: 6 }}>
              <WFCard style={{ flex: 1 }}><WFLabel>This season</WFLabel><div style={{ fontFamily: wfFontMono, fontSize: 18, marginTop: 4 }}>$340</div></WFCard>
              <WFCard style={{ flex: 1 }}><WFLabel>Pending</WFLabel><div style={{ fontFamily: wfFontMono, fontSize: 18, marginTop: 4 }}>$110</div></WFCard>
              <WFCard style={{ flex: 1 }}><WFLabel>From owner-sublet</WFLabel><div style={{ fontFamily: wfFontMono, fontSize: 18, marginTop: 4 }}>$240</div></WFCard>
            </div>
          </div>
        </div>
        {/* I'm Away modal */}
        <WFCard accent style={{ alignSelf: 'flex-start' }}>
          <WFTitle level={2}>I'll be away</WFTitle>
          <WFNote style={{ fontSize: 11 }}>Marinas can sublet your slip while you're gone. You earn 30% per the lease.</WFNote>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 7, marginTop: 10 }}>
            <WFInput label="From" value="Jun 14" />
            <WFInput label="To" value="Jun 24" />
            <div>
              <WFLabel>What should we do?</WFLabel>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 5, marginTop: 5, fontSize: 11 }}>
                <label style={{ display: 'flex', gap: 6, alignItems: 'flex-start' }}>
                  <span style={{ width: 11, height: 11, borderRadius: 99, border: `1.5px solid ${WF_INK}`, background: WF_ACCENT, marginTop: 2 }}></span>
                  <span><strong>Marina lists for me</strong> — easiest, 70/30 split</span>
                </label>
                <label style={{ display: 'flex', gap: 6, alignItems: 'flex-start' }}>
                  <span style={{ width: 11, height: 11, borderRadius: 99, border: `1.5px solid ${WF_INK}`, marginTop: 2 }}></span>
                  <span><strong>I'll list it myself</strong> — set my own price · 85/15</span>
                </label>
                <label style={{ display: 'flex', gap: 6, alignItems: 'flex-start' }}>
                  <span style={{ width: 11, height: 11, borderRadius: 99, border: `1.5px solid ${WF_INK}`, marginTop: 2 }}></span>
                  <span>Just block from search</span>
                </label>
              </div>
            </div>
            <WFButton primary style={{ marginTop: 6 }}>Mark me away</WFButton>
            <WFAnnotation rotate={3} style={{ position: 'static', textAlign: 'center', marginTop: 4 }}>
              ★ platform's killer differentiator
            </WFAnnotation>
          </div>
        </WFCard>
      </div>
    </WFPage>
  );
}

Object.assign(window, { FrameSearch, FrameSearchMarina, FrameSlipDetail, FrameBookingFlow, FrameBoaterDashboard, FrameMySlipAway });
