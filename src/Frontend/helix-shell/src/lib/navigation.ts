import type { ComponentType, SVGProps } from 'react';
import {
  AccountingIcon,
  DashboardIcon,
  InventoryIcon,
  SalesIcon,
  SettingsIcon,
  UsersIcon
} from '../components/icons';

export type NavKey = 'dashboard' | 'sales' | 'inventory' | 'accounting' | 'settings' | 'users';

export interface NavItem {
  key: NavKey;
  label: string;
  caption: string;
  icon: ComponentType<SVGProps<SVGSVGElement>>;
}

export interface NavSection {
  title: string;
  items: NavItem[];
}

export const navSections: NavSection[] = [
  {
    title: 'عمومی',
    items: [
      { key: 'dashboard', label: 'داشبورد', caption: 'نمای کلی', icon: DashboardIcon }
    ]
  },
  {
    title: 'ماژول‌ها',
    items: [
      { key: 'sales', label: 'فروش', caption: 'مشتریان و سفارش‌ها', icon: SalesIcon },
      { key: 'inventory', label: 'انبار', caption: 'کالا و موجودی', icon: InventoryIcon },
      { key: 'accounting', label: 'حسابداری', caption: 'دفتر و اسناد', icon: AccountingIcon }
    ]
  },
  {
    title: 'سیستم',
    items: [
      { key: 'users', label: 'کاربران و گروه‌ها', caption: 'دسترسی‌ها', icon: UsersIcon },
      { key: 'settings', label: 'تنظیمات', caption: 'پیکربندی پلتفرم', icon: SettingsIcon }
    ]
  }
];

export const allNavItems = navSections.flatMap((section) => section.items);

export function findNavItem(key: NavKey): NavItem {
  return allNavItems.find((item) => item.key === key) ?? allNavItems[0];
}