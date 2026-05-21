export interface Product {
  id: string;
  categoryId: string;
  categoryName: string;
  sku: string;
  name: string;
  description: string;
  brand: string;
  unitPrice: number;
  stockQuantity: number;
  reorderLevel: number;
  imageUrl: string | null;
  isActive: boolean;
  weightGrams: number;
  tags?: string[];
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  size: number;
  total: number;
  totalPages: number;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description: string | null;
}

