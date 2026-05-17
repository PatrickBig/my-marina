// Marina operator wireframes (5 frames)
// O1 Dashboard, O2 Slips & docks, O3 Reservation inbox, O4 Billing, O5 Listing calendar

const OP_NAV = ['Dashboard','Reservations','Slips & docks','Listings','Billing accounts','Invoices','Maintenance','Announcements','Vessels','Staff'];

function FrameOpDashboard() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Marina Owner" scenario="Big Bay Marina · Pro tier" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Dashboard" items={OP_NAV} title="Big Bay Marina" />
        <div style={{ flex: 1, padding: 16, overflow: 'hidden' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <WFTitle level={1}>Today, Tue Aug 5</WFTitle>
            <WFNote>3 arrivals · 2 departures · 5 listings live</WFNote>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 10, marginTop: 12 }}>
            {[
              ['Occupancy', '92 / 110', '84%'],
              ['Open invoices', '$8,420', '11 accts'],
              ['Pending res.', '4', 'request-to-book'],
              ['Mkt earnings MTD', '$3,180', '12 reservations'],
            ].map(([l, v, h]) => (
              <WFCard key={l}>
                <WFLabel>{l}</WFLabel>
                <div style={{ fontFamily: wfFontMono, fontSize: 18, marginTop: 4 }}>{v}</div>
                <WFNote style={{ fontSize: 10 }}>{h}</WFNote>
              </WFCard>
            ))}
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr', gap: 12, marginTop: 12 }}>
            <WFCard>
              <WFLabel>Today's arrivals</WFLabel>
              <div style={{ marginTop: 6 }}>
                {[
                  ['T. Reyes', 'Wanderlust 38\'', 'A-12 · 3pm', 'Confirmed'],
                  ['J. Lin', 'Halcyon 44\'', 'B-3 · 5pm', 'Confirmed'],
                  ['B. Park', 'Mara 32\'', 'D-8 · TBD', 'No-show?'],
                ].map(r => (
                  <div key={r[0]} style={{ display: 'flex', justifyContent: 'space-between', padding: '5px 0', borderBottom: `1px dashed ${WF_INK_FAINT}`, fontSize: 12 }}>
                    <div>{r[0]} · <span style={{ color: WF_INK_SOFT }}>{r[1]}</span></div>
                    <div style={{ fontFamily: wfFontMono, fontSize: 11 }}>{r[2]}</div>
                    <WFTag>{r[3]}</WFTag>
                  </div>
                ))}
              </div>
            </WFCard>
            <WFCard>
              <WFLabel>Action queue</WFLabel>
              <div style={{ marginTop: 6, display: 'flex', flexDirection: 'column', gap: 5, fontSize: 12 }}>
                <div>· 4 reservations need approval <WFTag>Request</WFTag></div>
                <div>· 2 host-marina approvals (dockominium) <WFTag style={{ background: WF_ACCENT_SOFT }}>Approval</WFTag></div>
                <div>· 1 holder marked away → list sublet? <WFTag style={{ background: WF_HIGHLIGHT }}>Sublet</WFTag></div>
                <div>· 3 invoices overdue &gt; 30d</div>
                <div>· 5 vessels with insurance expiring</div>
              </div>
            </WFCard>
          </div>
          <div style={{ marginTop: 12, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <WFCard>
              <WFLabel>Sublet activity (this week)</WFLabel>
              <WFNote style={{ fontSize: 11, marginTop: 4 }}>2 holders away · 1 owner-sublet listed · $340 split owed</WFNote>
              <WFButton small style={{ marginTop: 6 }}>Review →</WFButton>
            </WFCard>
            <WFCard>
              <WFLabel>Maintenance inbox</WFLabel>
              <WFNote style={{ fontSize: 11, marginTop: 4 }}>· Bilge pump · A-12 · in progress</WFNote>
              <WFNote style={{ fontSize: 11 }}>· Pedestal flickering · A-12 · new</WFNote>
            </WFCard>
          </div>
        </div>
      </div>
    </WFPage>
  );
}

function FrameOpSlips() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Marina Operator" scenario="Slips & docks" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Slips & docks" items={OP_NAV} title="Big Bay Marina" />
        <div style={{ flex: 1, padding: 16, overflow: 'hidden' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
            <WFTitle level={1}>Slips &amp; docks</WFTitle>
            <div style={{ display: 'flex', gap: 6 }}>
              <WFButton small>+ Dock</WFButton>
              <WFButton small primary>+ Slip</WFButton>
            </div>
          </div>
          <WFNote>5 docks · 110 slips · slip-map view post-MVP</WFNote>
          <div style={{ display: 'grid', gridTemplateColumns: '180px 1fr', gap: 12, marginTop: 10, height: 'calc(100% - 60px)' }}>
            {/* dock list */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
              <WFLabel>Docks</WFLabel>
              {[['A','22 / 24','active'],['B','18 / 20','active'],['C','19 / 22','active'],['D','15 / 18','active'],['Moorings','18 / 26','seasonal']].map((d,i) => (
                <WFCard key={d[0]} accent={i===0} style={{ padding: 8 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <strong>Dock {d[0]}</strong>
                    <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{d[1]}</span>
                  </div>
                  <WFNote style={{ fontSize: 10 }}>{d[2]}</WFNote>
                </WFCard>
              ))}
            </div>
            {/* slip table */}
            <div style={{ overflow: 'hidden' }}>
              <div style={{ display: 'flex', gap: 6, marginBottom: 6 }}>
                <WFInput value="Dock A · 24 slips" style={{ flex: 1 }} hint="search" />
                <WFPill>Active 22</WFPill>
                <WFPill>Maint 1</WFPill>
                <WFPill>Inactive 1</WFPill>
              </div>
              <WFCard padding={0}>
                <div style={{ display: 'grid', gridTemplateColumns: '60px 1fr 1fr 1fr 1fr 1fr 80px', padding: '6px 10px', fontFamily: wfFontMono, fontSize: 9, textTransform: 'uppercase', letterSpacing: 0.6, color: WF_INK_SOFT, borderBottom: `1px dashed ${WF_INK_FAINT}` }}>
                  <span>#</span><span>Type</span><span>Max L×B×D</span><span>Power</span><span>Status</span><span>Assignment</span><span></span>
                </div>
                {[
                  ['A-1','Floating',"32'×11'×5'",'30A','Active','Lewis seasonal'],
                  ['A-2','Floating',"32'×11'×5'",'30A','Active','— vacant'],
                  ['A-12','Floating',"42'×14'×6'",'50A','Active','Listed Aug 5–8'],
                  ['A-15','Floating',"42'×14'×6'",'50A','Maint','—'],
                  ['A-18','Fixed',"50'×16'×7'",'50A','Active','Park annual'],
                  ['A-22','Mooring',"40'×—×—",'—','Active','Vacant · listed'],
                ].map((r, i) => (
                  <div key={r[0]} style={{ display: 'grid', gridTemplateColumns: '60px 1fr 1fr 1fr 1fr 1fr 80px', padding: '7px 10px', borderBottom: i < 5 ? `1px dashed ${WF_INK_FAINT}` : 'none', fontSize: 11, alignItems: 'center' }}>
                    <strong style={{ fontFamily: wfFontMono }}>{r[0]}</strong>
                    <span>{r[1]}</span>
                    <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[2]}</span>
                    <span>{r[3]}</span>
                    <span><WFTag style={{ background: r[4]==='Maint' ? WF_HIGHLIGHT : WF_PAPER_LINE }}>{r[4]}</WFTag></span>
                    <span style={{ fontSize: 11 }}>{r[5]}</span>
                    <WFButton small>Edit</WFButton>
                  </div>
                ))}
              </WFCard>
              <WFAnnotation rotate={2} style={{ bottom: 30, right: 30 }}>
                slip-map view → post-MVP
              </WFAnnotation>
            </div>
          </div>
        </div>
      </div>
    </WFPage>
  );
}

function FrameOpReservations() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Marina Operator" scenario="Reservation inbox" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Reservations" items={OP_NAV} title="Big Bay Marina" />
        <div style={{ flex: 1, padding: 16, display: 'grid', gridTemplateColumns: '1fr 280px', gap: 12, overflow: 'hidden' }}>
          <div style={{ overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
            <WFTitle level={1}>Reservations</WFTitle>
            <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
              {['All 47','Pending 4','Confirmed 38','Today 3','Past 0','Cancelled 5'].map((t,i) => (
                <WFPill key={t} bg={i===1 ? WF_HIGHLIGHT : 'transparent'}>{t}</WFPill>
              ))}
            </div>
            <div style={{ marginTop: 8, overflow: 'auto', display: 'flex', flexDirection: 'column', gap: 6 }}>
              {[
                { who:'Tomás Reyes', boat:'Wanderlust 38\'', slip:'A-12', dates:'Aug 5–8', status:'Pending', focus:true, note:'Note: arriving 2pm via south channel' },
                { who:'Maria Rodriguez', boat:'Halcyon 44\'', slip:'A-12 (dockominium)', dates:'Aug 12–15', status:'Pending HM', desc:'Host marina approval (req)' },
                { who:'Jay Park', boat:'Skiff 24\'', slip:'B-3', dates:'Aug 6–7', status:'Confirmed' },
                { who:'L. Chen', boat:'Mariposa 36\'', slip:'D-8', dates:'Aug 8–14', status:'Confirmed' },
              ].map((r, i) => (
                <WFCard key={i} accent={r.focus}>
                  <div style={{ display: 'flex', gap: 10 }}>
                    <div style={{ width: 30, height: 30, borderRadius: 99, border: `1.5px solid ${WF_INK}`, fontFamily: wfFontMono, fontSize: 10, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>{r.who.split(' ').map(s => s[0]).join('')}</div>
                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                        <strong style={{ fontSize: 13 }}>{r.who}</strong>
                        <WFPill color={r.status==='Pending' ? WF_BAD : r.status==='Confirmed' ? WF_GOOD : WF_INK}>{r.status}</WFPill>
                      </div>
                      <WFNote style={{ fontSize: 11 }}>{r.boat} · {r.slip} · {r.dates}</WFNote>
                      {r.note && <WFNote style={{ fontSize: 11, marginTop: 3, fontStyle: 'italic' }}>"{r.note}"</WFNote>}
                      {r.desc && <WFTag style={{ marginTop: 4 }}>{r.desc}</WFTag>}
                      {r.focus && <div style={{ display: 'flex', gap: 6, marginTop: 6 }}>
                        <WFButton small primary>Approve</WFButton>
                        <WFButton small>Decline</WFButton>
                        <WFButton small>Message</WFButton>
                      </div>}
                    </div>
                  </div>
                </WFCard>
              ))}
            </div>
          </div>
          {/* Right: detail */}
          <WFCard accent>
            <WFLabel>Reservation #4821</WFLabel>
            <WFTitle level={2} style={{ marginTop: 4 }}>Tomás Reyes</WFTitle>
            <WFNote style={{ fontSize: 11 }}>Wanderlust · Catalina 38 · 38'×12'9"×6'</WFNote>
            <div style={{ display: 'flex', gap: 5, marginTop: 5 }}><WFTag>Insurance ✓</WFTag><WFTag style={{ background: WF_PAPER_LINE }}>1st visit</WFTag></div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <div style={{ fontSize: 12, lineHeight: 1.6 }}>
              <div><span style={{ color: WF_INK_SOFT }}>Slip</span> · A-12</div>
              <div><span style={{ color: WF_INK_SOFT }}>Dates</span> · Aug 5 → 8 (3 nts)</div>
              <div><span style={{ color: WF_INK_SOFT }}>Total</span> · $469 · off-platform</div>
              <div><span style={{ color: WF_INK_SOFT }}>Source</span> · marketplace</div>
              <div><span style={{ color: WF_INK_SOFT }}>Window</span> · request-to-book</div>
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <WFLabel>Status flow</WFLabel>
            <div style={{ fontFamily: wfFontMono, fontSize: 10, marginTop: 4 }}>
              Submitted → <strong style={{ background: WF_HIGHLIGHT }}>PendingApproval</strong> → Confirmed → Completed
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 10 }}>
              <WFButton primary small style={{ flex: 1 }}>Approve</WFButton>
              <WFButton small style={{ flex: 1 }}>Decline</WFButton>
            </div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

function FrameOpBilling() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Marina Operator" scenario="Billing accounts" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Billing accounts" items={OP_NAV} title="Big Bay Marina" />
        <div style={{ flex: 1, padding: 16, display: 'grid', gridTemplateColumns: '1.3fr 1fr', gap: 12, overflow: 'hidden' }}>
          <div style={{ overflow: 'hidden' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
              <WFTitle level={1}>Billing accounts</WFTitle>
              <WFButton small primary>+ New account</WFButton>
            </div>
            <WFNote>148 accounts · 23 ghost vessels awaiting claim</WFNote>
            <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
              <WFInput value="Search by name, email, vessel" style={{ flex: 1 }} />
              <WFPill>All</WFPill><WFPill>Active</WFPill><WFPill>Overdue</WFPill>
            </div>
            <WFCard padding={0} style={{ marginTop: 8 }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr 90px 90px 80px', padding: '6px 10px', fontFamily: wfFontMono, fontSize: 9, textTransform: 'uppercase', color: WF_INK_SOFT, borderBottom: `1px dashed ${WF_INK_FAINT}` }}>
                <span>Account</span><span>Slip</span><span>Balance</span><span>Vessel(s)</span><span>Status</span>
              </div>
              {[
                ['Reyes, Tomás · tomas@…','A-12 transient','$469','Wanderlust','Open'],
                ['Park, Jay · jay.p@…','B-3 seasonal','$0','Skiff','Active'],
                ['Lee Family · lee.tribe@…','D-8 annual','$1,240','Mariposa','Overdue'],
                ['Chen, L. · lc@…','C-7 (claim pending)','—','GHOST','Invited'],
                ['Rodriguez, Maria · maria@…','HOA only · A-12','$320','Halcyon (own slip)','Dockominium'],
              ].map((r, i) => (
                <div key={r[0]} style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr 90px 90px 80px', padding: '7px 10px', borderBottom: i<4 ? `1px dashed ${WF_INK_FAINT}` : 'none', fontSize: 11, alignItems: 'center', background: i===2 ? WF_ACCENT_SOFT : undefined }}>
                  <span>{r[0]}</span>
                  <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[1]}</span>
                  <span style={{ fontFamily: wfFontMono, color: r[2]==='—' ? WF_INK_FAINT : r[4]==='Overdue' ? WF_BAD : WF_INK }}>{r[2]}</span>
                  <span style={{ fontFamily: wfFontMono, fontSize: 10 }}>{r[3]}</span>
                  <WFTag style={{ background: r[4]==='Overdue' ? '#ffd' : r[4]==='Invited' ? WF_PAPER_LINE : WF_HIGHLIGHT }}>{r[4]}</WFTag>
                </div>
              ))}
            </WFCard>
          </div>
          {/* Right: account detail */}
          <WFCard accent>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <div>
                <WFLabel>Account</WFLabel>
                <WFTitle level={2}>Lee Family</WFTitle>
                <WFNote style={{ fontSize: 11 }}>3 members · slip D-8 annual</WFNote>
              </div>
              <WFPill color={WF_BAD}>Overdue</WFPill>
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <WFLabel>Members</WFLabel>
            <div style={{ fontSize: 11, marginTop: 4 }}>
              · L. Chen (Owner) lc@…<br />
              · A. Chen (CoOwner)<br />
              · M. Lee (Member · auto-pay TBD)
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <WFLabel>Vessels</WFLabel>
            <div style={{ fontSize: 11, marginTop: 4 }}>
              · Mariposa 36' · ins. ✓ exp 2026-04 (marina record)<br />
              · Tender 12' (ghost · awaiting claim)
            </div>
            <WFLine dashed style={{ margin: '10px 0' }} />
            <WFLabel>Open invoices</WFLabel>
            <div style={{ fontSize: 11, marginTop: 4 }}>
              · INV-2106 · $640 · 42d overdue<br />
              · INV-2188 · $600 · 12d overdue
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 10 }}>
              <WFButton small primary>+ Invoice</WFButton>
              <WFButton small>+ Payment</WFButton>
              <WFButton small>Message</WFButton>
            </div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

function FrameOpListing() {
  return (
    <WFPage>
      <WFPersonaRibbon persona="Marina Operator" scenario="Listing editor" />
      <div style={{ display: 'flex', height: '100%' }}>
        <WFSideRail active="Listings" items={OP_NAV} title="Big Bay Marina" />
        <div style={{ flex: 1, padding: 16, display: 'grid', gridTemplateColumns: '1fr 280px', gap: 12, overflow: 'hidden' }}>
          <div style={{ overflow: 'hidden' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
              <WFTitle level={1}>Listing · Slip A-12</WFTitle>
              <div style={{ display: 'flex', gap: 6 }}><WFButton small>Pause</WFButton><WFButton small primary>Save</WFButton></div>
            </div>
            <WFNote>Drag a date range on the calendar to set price &amp; policy. Multiple windows OK; no overlap.</WFNote>
            {/* calendar */}
            <div style={{ marginTop: 10, border: `1.5px solid ${WF_INK}`, filter: 'url(#wf-rough)', padding: 8 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                <strong style={{ fontFamily: wfFontHand, fontSize: 16 }}>August 2026</strong>
                <WFLabel>‹  ›</WFLabel>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7,1fr)', gap: 2, fontSize: 9, fontFamily: wfFontMono, textTransform: 'uppercase', color: WF_INK_SOFT }}>
                {['Su','Mo','Tu','We','Th','Fr','Sa'].map(d => <div key={d} style={{ textAlign: 'center', padding: 2 }}>{d}</div>)}
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7,1fr)', gap: 2, marginTop: 2 }}>
                {Array.from({ length: 35 }).map((_, i) => {
                  const day = i - 5;
                  const valid = day >= 1 && day <= 31;
                  // Window 1: Aug 1-15 $148 · Window 2: Aug 16-31 paused
                  const w1 = valid && day <= 15;
                  const w2 = valid && day >= 16 && day <= 31;
                  const booked = valid && [5,6,7].includes(day);
                  return (
                    <div key={i} style={{
                      height: 38,
                      border: `1px solid ${valid ? WF_INK_FAINT : 'transparent'}`,
                      background: booked ? WF_INK : w1 ? WF_ACCENT_SOFT : w2 ? WF_PAPER_LINE : '#fff',
                      color: booked ? '#fff' : WF_INK,
                      padding: 3,
                      fontSize: 10,
                      position: 'relative',
                      opacity: valid ? 1 : 0.3,
                    }}>
                      <div style={{ fontFamily: wfFontMono, fontWeight: 600 }}>{valid ? day : ''}</div>
                      {booked && <div style={{ fontSize: 8, fontFamily: wfFontMono }}>booked</div>}
                      {w1 && !booked && day !== 1 && day % 4 === 0 && <div style={{ fontSize: 8, fontFamily: wfFontMono }}>$148</div>}
                    </div>
                  );
                })}
              </div>
              <div style={{ display: 'flex', gap: 10, marginTop: 8, fontSize: 10, fontFamily: wfFontMono }}>
                <span><span style={{ display: 'inline-block', width: 10, height: 10, background: WF_ACCENT_SOFT, border: `1px solid ${WF_INK_FAINT}` }}></span> Window 1 · open</span>
                <span><span style={{ display: 'inline-block', width: 10, height: 10, background: WF_PAPER_LINE, border: `1px solid ${WF_INK_FAINT}` }}></span> Window 2 · paused</span>
                <span><span style={{ display: 'inline-block', width: 10, height: 10, background: WF_INK }}></span> booked</span>
              </div>
            </div>
            <WFAnnotation rotate={-2} style={{ bottom: 60, left: 250 }}>
              ← AvailabilityWindow (no overlap)
            </WFAnnotation>
          </div>
          {/* Right: window editor */}
          <WFCard accent>
            <WFLabel>Window 1 · Aug 1–15</WFLabel>
            <WFTitle level={2}>Pricing &amp; policy</WFTitle>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 7, marginTop: 8 }}>
              <WFInput label="Base / night" value="$148" />
              <div style={{ display: 'flex', gap: 6 }}>
                <WFInput label="Weekly −" value="10%" style={{ flex: 1 }} />
                <WFInput label="Monthly −" value="20%" style={{ flex: 1 }} />
              </div>
              <WFInput label="Cleaning fee" value="$25" />
              <div style={{ display: 'flex', gap: 6 }}>
                <WFInput label="Min nts" value="2" style={{ flex: 1 }} />
                <WFInput label="Max nts" value="14" style={{ flex: 1 }} />
              </div>
              <WFLine dashed />
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12 }}>
                <span>Instant book</span><WFToggle on />
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12 }}>
                <span>Status</span><WFTag>Open</WFTag>
              </div>
              <WFLine dashed />
              <WFLabel>Revenue split</WFLabel>
              <div style={{ fontFamily: wfFontMono, fontSize: 10 }}>
                SlipOwner 95%<br />Platform 5%<br />
                <span style={{ color: WF_INK_FAINT }}>(snapshotted at booking)</span>
              </div>
            </div>
          </WFCard>
        </div>
      </div>
    </WFPage>
  );
}

Object.assign(window, { FrameOpDashboard, FrameOpSlips, FrameOpReservations, FrameOpBilling, FrameOpListing });
