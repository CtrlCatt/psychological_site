import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
export type IconName = 'arrow' | 'brain' | 'calendar' | 'check' | 'chevron' | 'clock' | 'heart' | 'menu' | 'play' | 'quote' | 'shield' | 'spark' | 'star' | 'video' | 'x';
@Component({selector:'app-icon', standalone:true, templateUrl:'./icon.component.html', changeDetection:ChangeDetectionStrategy.OnPush, styles:[':host{display:inline-flex; flex-shrink:0; vertical-align:middle}']})
export class IconComponent { @Input({required:true}) name!: IconName; @Input() className = 'size-5'; }
