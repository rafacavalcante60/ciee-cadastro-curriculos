import { Pipe, PipeTransform } from '@angular/core';

// A API grava só os dígitos; a máscara existe apenas na exibição.
@Pipe({ name: 'telefone' })
export class TelefonePipe implements PipeTransform {
  transform(digitos: string | null): string | null {
    const partes = digitos?.match(/^(\d{2})(\d{4,5})(\d{4})$/);
    return partes ? `(${partes[1]}) ${partes[2]}-${partes[3]}` : digitos;
  }
}
