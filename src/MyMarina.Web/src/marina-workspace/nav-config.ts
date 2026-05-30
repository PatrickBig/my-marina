import {
  LayoutGrid, CalendarCheck, Wrench, Calendar,
  Users, ListChecks, Receipt,
  Anchor, DollarSign, Megaphone, Shield, Settings,
  Menu, FileText,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { MarinaCounters } from './useMarinaCounters';

export type NavId =
  | 'dashboard' | 'reservations' | 'inquiries' | 'maintenance' | 'listings'
  | 'accounts' | 'assignments' | 'billing'
  | 'slips' | 'pricing' | 'announcements' | 'staff' | 'settings';

export interface NavItem {
  id: NavId;
  label: string;
  icon: LucideIcon;
  counter: keyof MarinaCounters | null;
}

export interface NavGroup {
  label: string;
  items: NavItem[];
}

export const MARINA_NAV_GROUPS: NavGroup[] = [
  {
    label: 'Operations',
    items: [
      { id: 'dashboard',    label: 'Dashboard',     icon: LayoutGrid,    counter: null },
      { id: 'reservations', label: 'Reservations',  icon: CalendarCheck, counter: 'pendingReservations' },
      { id: 'inquiries',    label: 'Inquiries',     icon: FileText,      counter: 'pendingInquiries' },
      { id: 'maintenance',  label: 'Maintenance',   icon: Wrench,        counter: 'openWorkOrders' },
      { id: 'listings',     label: 'Listings',      icon: Calendar,      counter: null },
    ],
  },
  {
    label: 'Customers & money',
    items: [
      { id: 'accounts',     label: 'Customers',     icon: Users,         counter: null },
      { id: 'assignments',  label: 'Assignments',   icon: ListChecks,    counter: null },
      { id: 'billing',      label: 'Billing',       icon: Receipt,       counter: 'overdueInvoices' },
    ],
  },
  {
    label: 'Marina setup',
    items: [
      { id: 'slips',         label: 'Slips & docks', icon: Anchor,       counter: null },
      { id: 'pricing',       label: 'Pricing plans', icon: DollarSign,   counter: null },
      { id: 'announcements', label: 'Announcements', icon: Megaphone,    counter: null },
      { id: 'staff',         label: 'Staff',         icon: Shield,       counter: null },
      { id: 'settings',      label: 'Settings',      icon: Settings,     counter: null },
    ],
  },
];

export interface MobileTab {
  id: NavId | 'more';
  label: string;
  icon: LucideIcon;
  counter?: keyof MarinaCounters;
}

export const MOBILE_TABS: MobileTab[] = [
  { id: 'dashboard',    label: 'Home',    icon: LayoutGrid },
  { id: 'reservations', label: 'Res',     icon: CalendarCheck, counter: 'pendingReservations' },
  { id: 'slips',        label: 'Slips',   icon: Anchor },
  { id: 'billing',      label: 'Billing', icon: Receipt,       counter: 'overdueInvoices' },
  { id: 'more',         label: 'More',    icon: Menu },
];
